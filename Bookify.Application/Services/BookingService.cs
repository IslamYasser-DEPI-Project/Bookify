using Bookify.Application.DTOs.Responses;
using Bookify.Application.DTOs.ViewModels;
using Bookify.Application.Exceptions;
using Bookify.Application.Interfaces;
using Bookify.DA.Contracts;
using Bookify.DA.Entities;
using Bookify.DA.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bookify.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _uow;

        public BookingService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<BookingDto> CreateBookingAsync(string userId, CartViewModel cart)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId required", nameof(userId));

            if (cart?.Items == null || cart.Items.Count == 0)
                throw new BookingException("Cart is empty");

            // confirm customer exists
            var customer = await _uow.CustomerRepository
                .GetAllQueryable()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                throw new BookingException("Customer not found for user");

            // validate availability
            foreach (var item in cart.Items)
            {
                var ok = await _uow.BookingRepository.IsRoomAvailableAsync(item.RoomId, item.CheckInDate, item.CheckOutDate);
                if (!ok)
                    throw new BookingException($"Room {item.RoomId} is not available for requested dates.");
            }

            // Create bookings (all added before SaveChanges -> single SaveChangesAsync is transactional)
            var createdBookings = new List<Booking>();
            foreach (var item in cart.Items)
            {
                var booking = new Booking
                {
                    CustomerID = customer.Id,
                    RoomID = item.RoomId,
                    BookingDate = DateTime.UtcNow,
                    CheckInDate = item.CheckInDate,
                    CheckOutDate = item.CheckOutDate,
                    Status = BookingStatus.Pending
                };
                await _uow.BookingRepository.Add(booking);
                createdBookings.Add(booking);
            }

            await _uow.SaveChangesAsync();

            // Map first created booking (method contract returns single BookingDto). If you want multiple, change signature.
            var b = createdBookings.First();
            return new BookingDto
            {
                Id = b.Id,
                BookingNumber = b.Id.ToString(),
                CustomerId = b.CustomerID,
                RoomId = b.RoomID,
                RoomNumber = (await _uow.RoomRepository.GetById(b.RoomID))?.RoomNumber ?? string.Empty,
                BookingDate = b.BookingDate,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                Status = b.Status.ToString()
            };
        }

        public async Task<bool> ValidateCartAvailabilityAsync(CartViewModel cart)
        {
            if (cart?.Items == null || cart.Items.Count == 0) return false;

            foreach (var item in cart.Items)
            {
                var ok = await _uow.BookingRepository.IsRoomAvailableAsync(item.RoomId, item.CheckInDate, item.CheckOutDate);
                if (!ok) return false;
            }

            return true;
        }

        public async Task<IEnumerable<BookingDto>> GetUserBookingsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return Array.Empty<BookingDto>();

            var bookings = await _uow.BookingRepository.GetUserBookingsAsync(userId);
            return bookings.Select(b => new BookingDto
            {
                Id = b.Id,
                BookingNumber = b.Id.ToString(),
                CustomerId = b.CustomerID,
                RoomId = b.RoomID,
                RoomNumber = b.Room?.RoomNumber ?? string.Empty,
                BookingDate = b.BookingDate,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                Status = b.Status.ToString()
            });
        }

        public async Task<BookingDto?> GetBookingByNumberAsync(string bookingNumber)
        {
            var b = await _uow.BookingRepository.GetByBookingNumberAsync(bookingNumber);
            if (b == null) return null;
            return new BookingDto
            {
                Id = b.Id,
                BookingNumber = b.Id.ToString(),
                CustomerId = b.CustomerID,
                RoomId = b.RoomID,
                RoomNumber = b.Room?.RoomNumber ?? string.Empty,
                BookingDate = b.BookingDate,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                Status = b.Status.ToString()
            };
        }

        public async Task<bool> CancelBookingAsync(int bookingId, string userId)
        {
            var booking = await _uow.BookingRepository.GetById(bookingId);
            if (booking == null) return false;

            // confirm ownership
            var customer = await _uow.CustomerRepository.GetByUserIdAsync(userId);
            if (customer == null || booking.CustomerID != customer.Id)
                return false;

            booking.Status = BookingStatus.Cancelled;
            await _uow.BookingRepository.Update(booking);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConfirmPaymentAsync(string bookingNumber, string stripePaymentIntentId)
        {
            // Minimal implementation: mark booking as Confirmed. Payment recording/stripe handling should go in PaymentService.
            var booking = await _uow.BookingRepository.GetByBookingNumberAsync(bookingNumber);
            if (booking == null) return false;

            booking.Status = BookingStatus.Confirmed;
            await _uow.BookingRepository.Update(booking);
            await _uow.SaveChangesAsync();

            // TODO: create Payment entity here referencing stripePaymentIntentId and call PaymentRepository.Add(...)
            return true;
        }
    }
}
