using Bookify.Application.DTOs.Responses;
using Bookify.Application.DTOs.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookify.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingDto> CreateBookingAsync(string userId, CartViewModel cart);
        Task<bool> ValidateCartAvailabilityAsync(CartViewModel cart);
        Task<IEnumerable<BookingDto>> GetUserBookingsAsync(string userId);
        Task<BookingDto?> GetBookingByNumberAsync(string bookingNumber);
        Task<bool> CancelBookingAsync(int bookingId, string userId);
        Task<bool> ConfirmPaymentAsync(string bookingNumber, string stripePaymentIntentId);
    }
}
