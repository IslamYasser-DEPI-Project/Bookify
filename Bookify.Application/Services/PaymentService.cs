using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bookify.Application.DTOs.ViewModels;
using Bookify.Application.Interfaces;
using Bookify.DA.Contracts;
using Bookify.DA.Entities;
using Bookify.DA.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Bookify.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _uow;
        private readonly string _stripeSecretKey;
        private readonly string _webhookSecret;
        private readonly string _successUrl;
        private readonly string _cancelUrl;

        public PaymentService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _uow = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            // Prefer the server secret key. If only a publishable key (pk_) is present, fail with a clear message.
            _stripeSecretKey = configuration["Stripe:SecretKey"] ?? string.Empty;
            var publishableKey = configuration["Stripe:ApiKey"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_stripeSecretKey))
            {
                if (!string.IsNullOrWhiteSpace(publishableKey))
                {
                    throw new InvalidOperationException("Stripe secret key not configured. A publishable (pk_) key was found, but server-side Stripe operations require a secret (sk_) key. Set Stripe:SecretKey in configuration.");
                }

                throw new InvalidOperationException("Stripe secret key not configured. Set Stripe:SecretKey in configuration.");
            }

            if (!_stripeSecretKey.StartsWith("sk_"))
            {
                throw new InvalidOperationException("Configured Stripe secret key does not appear to be a secret (sk_) key. Verify your Stripe:SecretKey value.");
            }

            _webhookSecret = configuration["Stripe:WebhookSecret"] ?? string.Empty;
            _successUrl = configuration["Stripe:SuccessUrl"] ?? "https:///payment-success?session_id={CHECKOUT_SESSION_ID}";
            _cancelUrl = configuration["Stripe:CancelUrl"] ?? "https:///payment-cancel";

            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        public async Task<string> CreateCheckoutSessionAsync(CartViewModel cart, string bookingNumber, string userId, string? customerEmail = null)
        {
            if (cart?.Items == null || cart.Items.Count == 0)
                return string.Empty;

            var lineItems = new List<SessionLineItemOptions>();

            foreach (var item in cart.Items)
            {
                var room = await _uow.RoomRepository.GetWithImagesAsync(item.RoomId);
                if (room == null || room.RoomType == null) continue;

                var nights = Math.Max(1, (item.CheckOutDate - item.CheckInDate).Days);
                var unitAmount = (long)Math.Round(room.RoomType.PricePerNight * 100m);

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd", // consider making this configurable on CartViewModel
                        UnitAmount = unitAmount,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{room.RoomNumber} - {room.RoomType.Name}",
                            Description = $"Check-in: {item.CheckInDate:yyyy-MM-dd}, Check-out: {item.CheckOutDate:yyyy-MM-dd}"
                        }
                    },
                    Quantity = nights
                });
            }

            if (lineItems.Count == 0) return string.Empty;

            var options = new SessionCreateOptions
            {
                SuccessUrl = _successUrl,
                CancelUrl = _cancelUrl,
                LineItems = lineItems,
                Mode = "payment",
                Metadata = new Dictionary<string, string>
                {
                    { "bookingNumber", bookingNumber },
                    { "userId", userId }
                }
            };

            if (!string.IsNullOrWhiteSpace(customerEmail))
                options.CustomerEmail = customerEmail;

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            return session.Url ?? string.Empty;
        }

        public async Task<string> CreatePaymentIntentAsync(decimal amount, string currency, string bookingNumber)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)Math.Round(amount * 100m),
                Currency = (currency ?? "usd").ToLower(),
                Metadata = new Dictionary<string, string>
                {
                    { "bookingNumber", bookingNumber }
                },
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);
            return paymentIntent.ClientSecret ?? string.Empty;
        }

        public Task<bool> VerifyWebhookSignatureAsync(string payload, string signature)
        {
            if (string.IsNullOrWhiteSpace(_webhookSecret))
                return Task.FromResult(false);

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret);
                return Task.FromResult(stripeEvent != null);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public async Task ProcessPaymentWebhookAsync(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return;

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(payload);
            }
            catch
            {
                return;
            }

            if (!doc.RootElement.TryGetProperty("type", out var typeProp))
                return;

            var eventType = typeProp.GetString();

            try
            {
                if (eventType == "checkout.session.completed")
                {
                    var session = doc.RootElement.GetProperty("data").GetProperty("object");
                    var sessionId = session.GetProperty("id").GetString();
                    var paymentStatus = session.GetProperty("payment_status").GetString();

                    string? bookingNumber = null;
                    if (session.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object)
                    {
                        if (metadata.TryGetProperty("bookingNumber", out var bn))
                            bookingNumber = bn.GetString();
                    }

                    if (!string.IsNullOrWhiteSpace(bookingNumber) && paymentStatus == "paid")
                    {
                        var booking = await _uow.BookingRepository.GetByBookingNumberAsync(bookingNumber);
                        if (booking != null)
                        {
                            // compute amount from room type if possible
                            decimal amount = 0m;
                            var room = await _uow.RoomRepository.GetWithImagesAsync(booking.RoomID);
                            if (room?.RoomType != null)
                            {
                                var nights = Math.Max(1, (booking.CheckOutDate - booking.CheckInDate).Days);
                                amount = room.RoomType.PricePerNight * nights;
                            }


                            var stripeType = await _uow.PaymentTypeRepository
                                .GetAllQueryable()
                                .FirstOrDefaultAsync(pt => pt.TypeName == "Stripe");

                            if (stripeType == null)
                            {
                                stripeType = new PaymentType { TypeName = "Stripe", Description = "Stripe payments" };
                                await _uow.PaymentTypeRepository.Add(stripeType);
                                await _uow.SaveChangesAsync();
                            }


                            var existing = (await _uow.PaymentRepository.GetByBookingIdAsync(booking.Id))
                                .FirstOrDefault(p => p.PaymentTypeID == stripeType.Id /* further checks possible */);

                            if (existing == null)
                            {
                                var payment = new Payment
                                {
                                    BookingID = booking.Id,
                                    PaymentTypeID = stripeType.Id,
                                    Amount = Math.Round(amount, 2),
                                    Date = DateTime.UtcNow,
                                    PaymentStatus = PaymentStatus.Completed
                                };

                                await _uow.PaymentRepository.Add(payment);
                            }
                            else
                            {
                                // optionally update existing payment status/amount
                                existing.PaymentStatus = PaymentStatus.Completed;
                                existing.Amount = Math.Round(amount, 2);
                                await _uow.PaymentRepository.Update(existing);
                            }

                            booking.Status = BookingStatus.Confirmed;
                            await _uow.BookingRepository.Update(booking);

                            await _uow.SaveChangesAsync();
                        }
                    }
                }
                else if (eventType == "payment_intent.succeeded")
                {
                    var pi = doc.RootElement.GetProperty("data").GetProperty("object");
                    string? bookingNumber = null;
                    if (pi.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object)
                    {
                        if (metadata.TryGetProperty("bookingNumber", out var bn))
                            bookingNumber = bn.GetString();
                    }

                    var intentId = pi.GetProperty("id").GetString();
                    long amountReceivedCents = 0;
                    if (pi.TryGetProperty("amount_received", out var ar) && ar.TryGetInt64(out var arVal))
                        amountReceivedCents = arVal;

                    if (!string.IsNullOrWhiteSpace(bookingNumber))
                    {
                        var booking = await _uow.BookingRepository.GetByBookingNumberAsync(bookingNumber);
                        if (booking != null)
                        {
                            decimal amount = amountReceivedCents / 100m;

                            var stripeType = await _uow.PaymentTypeRepository
                                .GetAllQueryable()
                                .FirstOrDefaultAsync(pt => pt.TypeName == "Stripe");

                            if (stripeType == null)
                            {
                                stripeType = new PaymentType { TypeName = "Stripe", Description = "Stripe payments" };
                                await _uow.PaymentTypeRepository.Add(stripeType);
                                await _uow.SaveChangesAsync();
                            }

                            // Avoid duplicates
                            var existing = (await _uow.PaymentRepository.GetByBookingIdAsync(booking.Id))
                                .FirstOrDefault(p => p.PaymentTypeID == stripeType.Id /* further checks possible */);

                            if (existing == null)
                            {
                                var payment = new Payment
                                {
                                    BookingID = booking.Id,
                                    PaymentTypeID = stripeType.Id,
                                    Amount = Math.Round(amount, 2),
                                    Date = DateTime.UtcNow,
                                    PaymentStatus = PaymentStatus.Completed
                                };
                                await _uow.PaymentRepository.Add(payment);
                            }
                            else
                            {
                                existing.PaymentStatus = PaymentStatus.Completed;
                                existing.Amount = Math.Round(amount, 2);
                                await _uow.PaymentRepository.Update(existing);
                            }

                            booking.Status = BookingStatus.Confirmed;
                            await _uow.BookingRepository.Update(booking);

                            await _uow.SaveChangesAsync();
                        }
                    }
                }
            }
            catch
            {
                //logging
            }
        }
    }
}