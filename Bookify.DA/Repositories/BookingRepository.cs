using Bookify.DA.Contracts.RepositoryContracts;
using Bookify.DA.Data;
using Bookify.DA.Entities;
using Bookify.DA.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.DA.Repositories
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        public BookingRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null)
        {
            if (checkIn >= checkOut)
                throw new ArgumentException("checkIn must be before checkOut", nameof(checkIn));

            var overlappingBookings = GetAllQueryable()
                .Where(b => b.RoomID == roomId
                            && (excludeBookingId == null || b.Id != excludeBookingId.Value)
                            && b.Status != BookingStatus.Cancelled
                            && b.CheckInDate < checkOut
                            && b.CheckOutDate > checkIn);

            return !await overlappingBookings.AnyAsync();
        }

        public async Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Array.Empty<Booking>();

            return await GetAllQueryable()
                .Include(b => b.Room)
                .Include(b => b.Customer)
                .Where(b => b.Customer != null && b.Customer.UserId == userId)
                .ToListAsync();
        }

        public async Task<Booking?> GetByBookingNumberAsync(string bookingNumber)
        {
            if (string.IsNullOrWhiteSpace(bookingNumber))
                return null;

            // Note: Booking entity currently has no BookingNumber property.
            // Fallback: try parse bookingNumber to Id. If you add a BookingNumber property,
            // replace this lookup with the property-based lookup.
            if (int.TryParse(bookingNumber, out var id))
            {
                return await GetAllQueryable()
                    .Include(b => b.Room)
                    .Include(b => b.Customer)
                    .FirstOrDefaultAsync(b => b.Id == id);
            }

            return null;
        }

        public async Task<IEnumerable<Booking>> GetBookingsInRangeAsync(DateTime from, DateTime to)
        {
            if (from >= to)
                return Array.Empty<Booking>();

            return await GetAllQueryable()
                .Include(b => b.Room)
                .Include(b => b.Customer)
                // overlap with range [from, to): b.CheckIn < to && b.CheckOut > from
                .Where(b => b.CheckInDate < to && b.CheckOutDate > from)
                .ToListAsync();
        }
    }
}
