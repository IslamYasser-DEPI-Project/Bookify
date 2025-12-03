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
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<PaymentService> logger)
        {
            _uow = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));


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
                    { "bookingNumber", bookingNumber ?? string.Empty },
                    { "userId", userId ?? string.Empty }
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stripe webhook signature verification failed.");
                return Task.FromResult(false);
            }
        }

        public async Task ProcessPaymentWebhookAsync(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                _logger.LogWarning("Empty webhook payload.");
                return;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid webhook JSON payload.");
                return;
            }

            if (!doc.RootElement.TryGetProperty("type", out var typeProp))
            {
                _logger.LogWarning("Webhook event missing 'type' property.");
                return;
            }

            var eventType = typeProp.GetString();
            _logger.LogInformation("Processing Stripe webhook event type={EventType}", eventType);

            try
            {

                async Task<Booking?> ResolveBookingFromJsonElementAsync(JsonElement metadataElement)
                {
                    string? bookingNumber = null;
                    string? userId = null;

                    if (metadataElement.ValueKind == JsonValueKind.Object)
                    {
                        if (metadataElement.TryGetProperty("bookingNumber", out var bnEl) && bnEl.ValueKind == JsonValueKind.String)
                            bookingNumber = bnEl.GetString();

                        if (metadataElement.TryGetProperty("userId", out var uidEl) && uidEl.ValueKind == JsonValueKind.String)
                            userId = uidEl.GetString();
                    }

                    if (!string.IsNullOrWhiteSpace(bookingNumber) && int.TryParse(bookingNumber, out var bookingId))
                    {
                        var b = await _uow.BookingRepository.GetById(bookingId);
                        if (b != null) return b;
                    }

                    if (!string.IsNullOrWhiteSpace(userId))
                    {

                        var pending = await _uow.BookingRepository
                            .GetAllQueryable()
                            .Include(b => b.Customer)
                            .Include(b => b.Room)
                            .Where(b => b.Customer != null && b.Customer.UserId == userId && b.Status == BookingStatus.Pending)
                            .OrderByDescending(b => b.BookingDate)
                            .FirstOrDefaultAsync();

                        if (pending != null) return pending;
                    }

                    return null;
                }

                if (eventType == "checkout.session.completed")
                {
                    var session = doc.RootElement.GetProperty("data").GetProperty("object");
                    var paymentStatus = session.GetProperty("payment_status").GetString();
                    string? sessionId = null;
                    if (session.TryGetProperty("id", out var idEl)) sessionId = idEl.GetString();

                    JsonElement metadataEl = default;
                    if (session.TryGetProperty("metadata", out var metadata))
                        metadataEl = metadata;

                    var booking = await ResolveBookingFromJsonElementAsync(metadataEl);

                    if (booking == null)
                    {
                        _logger.LogWarning("No booking found for checkout.session.completed (sessionId={SessionId}). Metadata: {Metadata}", sessionId, metadataEl.ToString());
                        return;
                    }

                    if (paymentStatus == "paid")
                    {
                        await UpsertPaymentAndConfirmBookingAsync(booking, "Stripe", amountFromBooking: null);
                    }
                    else
                    {
                        _logger.LogInformation("checkout.session.completed with payment_status={PaymentStatus} for bookingId={BookingId}", paymentStatus, booking.Id);
                    }
                }
                else if (eventType == "payment_intent.succeeded")
                {
                    var pi = doc.RootElement.GetProperty("data").GetProperty("object");
                    JsonElement metadataEl = default;
                    if (pi.TryGetProperty("metadata", out var metadata))
                        metadataEl = metadata;

                    string? bookingNumber = null;
                    if (metadataEl.ValueKind == JsonValueKind.Object && metadataEl.TryGetProperty("bookingNumber", out var bn) && bn.ValueKind == JsonValueKind.String)
                        bookingNumber = bn.GetString();

                    // attempt to resolve booking
                    Booking? booking = null;
                    if (!string.IsNullOrWhiteSpace(bookingNumber) && int.TryParse(bookingNumber, out var bid))
                    {
                        booking = await _uow.BookingRepository.GetById(bid);
                    }

                    if (booking == null)
                    {
                        // fallback to user id in metadata
                        if (metadataEl.ValueKind == JsonValueKind.Object && metadataEl.TryGetProperty("userId", out var uid) && uid.ValueKind == JsonValueKind.String)
                        {
                            var userId = uid.GetString();
                            booking = await _uow.BookingRepository
                                .GetAllQueryable()
                                .Include(b => b.Customer)
                                .Include(b => b.Room)
                                .Where(b => b.Customer != null && b.Customer.UserId == userId && b.Status == BookingStatus.Pending)
                                .OrderByDescending(b => b.BookingDate)
                                .FirstOrDefaultAsync();
                        }
                    }

                    if (booking == null)
                    {
                        _logger.LogWarning("No booking found for payment_intent.succeeded. Metadata: {Metadata}", metadataEl.ToString());
                        return;
                    }

                    long amountReceivedCents = 0;
                    if (pi.TryGetProperty("amount_received", out var ar) && ar.TryGetInt64(out var arVal))
                        amountReceivedCents = arVal;

                    decimal amount = amountReceivedCents > 0 ? amountReceivedCents / 100m : 0m;

                    await UpsertPaymentAndConfirmBookingAsync(booking, "Stripe", amountFromBooking: amount);
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing Stripe webhook event.");
            }
        }

        private async Task UpsertPaymentAndConfirmBookingAsync(Booking booking, string paymentTypeName, decimal? amountFromBooking = null)
        {
            if (booking == null) throw new ArgumentNullException(nameof(booking));

            try
            {

                decimal amount = 0m;
                var room = await _uow.RoomRepository.GetWithImagesAsync(booking.RoomID);
                if (room?.RoomType != null)
                {
                    var nights = Math.Max(1, (booking.CheckOutDate - booking.CheckInDate).Days);
                    amount = room.RoomType.PricePerNight * nights;
                }

                if (amountFromBooking.HasValue && amountFromBooking.Value > 0m)
                    amount = amountFromBooking.Value;

                // ensure PaymentType exists
                var stripeType = await _uow.PaymentTypeRepository
                    .GetAllQueryable()
                    .FirstOrDefaultAsync(pt => pt.TypeName == paymentTypeName);

                if (stripeType == null)
                {
                    stripeType = new PaymentType { TypeName = paymentTypeName, Description = $"{paymentTypeName} payments" };
                    await _uow.PaymentTypeRepository.Add(stripeType);
                    await _uow.SaveChangesAsync();
                }

                // check existing payment for booking & type to avoid duplicates
                var existingPayments = (await _uow.PaymentRepository.GetByBookingIdAsync(booking.Id)).ToList();
                var existing = existingPayments.FirstOrDefault(p => p.PaymentTypeID == stripeType.Id);

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
                    _logger.LogInformation("Adding Payment for bookingId={BookingId}, amount={Amount}", booking.Id, payment.Amount);
                }
                else
                {
                    existing.PaymentStatus = PaymentStatus.Completed;
                    existing.Amount = Math.Round(amount, 2);
                    await _uow.PaymentRepository.Update(existing);
                    _logger.LogInformation("Updated existing Payment (Id={PaymentId}) for bookingId={BookingId}", existing.Id, booking.Id);
                }


                booking.Status = BookingStatus.Confirmed;
                await _uow.BookingRepository.Update(booking);

                // commit
                await _uow.SaveChangesAsync();
                _logger.LogInformation("Payment recorded and booking confirmed for bookingId={BookingId}", booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert payment and confirm booking for bookingId={BookingId}", booking.Id);
                throw;
            }
        }
    }
}
