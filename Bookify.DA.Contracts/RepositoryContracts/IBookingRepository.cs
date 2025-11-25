using Bookify.DA.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookify.DA.Contracts.RepositoryContracts
{
    public interface IBookingRepository : IGenericRepository<Booking>
    {
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null);
        Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId);
        Task<Booking?> GetByBookingNumberAsync(string bookingNumber);
        Task<IEnumerable<Booking>> GetBookingsInRangeAsync(DateTime from, DateTime to);
    }
}   