using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bookify.DA.Contracts.RepositoryContracts;
using Bookify.DA.Data;
using Bookify.DA.Entities;
using Microsoft.EntityFrameworkCore;
using Bookify.DA.Enums;

namespace Bookify.DA.Repositories
{
    public class RoomRepository : GenericRepository<Room>, IRoomRepository
    {
        public RoomRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, object? filter = null)
        {
            if (checkIn >= checkOut)
                throw new ArgumentException("checkIn must be before checkOut", nameof(checkIn));

            var rooms = GetAllQueryable()
                .Include(r => r.RoomType)
                .Include(r => r.Hotel)
                .Include(r => r.Bookings)
                .Where(r => r.Status == RoomStatus.Available);

            // Filter out rooms that have overlapping non-cancelled bookings
            rooms = rooms.Where(r => !r.Bookings.Any(b =>
                b.Status != BookingStatus.Cancelled
                && b.CheckInDate < checkOut
                && b.CheckOutDate > checkIn));

            //int -> RoomTypeID, string -> RoomNumber contains
            if (filter is int roomTypeId)
            {
                rooms = rooms.Where(r => r.RoomTypeID == roomTypeId);
            }
            else if (filter is string s && !string.IsNullOrWhiteSpace(s))
            {
                var q = s.Trim();
                rooms = rooms.Where(r => r.RoomNumber.Contains(q));
            }

            return await rooms.ToListAsync();
        }

        public async Task<IEnumerable<Room>> GetFeaturedRoomsAsync(int count)
        {
            if (count <= 0)
                return Array.Empty<Room>();

            return await GetAllQueryable()
                .Include(r => r.RoomType)
                .Include(r => r.Hotel)
                .OrderBy(r => r.Id) 
                .Take(count)
                .ToListAsync();
        }

        public async Task<Room?> GetWithImagesAsync(int id)
        {
            
            return await GetAllQueryable()
                .Include(r => r.RoomType)
                .Include(r => r.Hotel)
                .Include(r => r.Bookings)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null)
        {
            if (checkIn >= checkOut)
                throw new ArgumentException("checkIn must be before checkOut !", nameof(checkIn));

            var overlapping = _context.Bookings
                .Where(b => b.RoomID == roomId
                            && (excludeBookingId == null || b.Id != excludeBookingId.Value)
                            && b.Status != BookingStatus.Cancelled
                            && b.CheckInDate < checkOut
                            && b.CheckOutDate > checkIn);

            return !await overlapping.AnyAsync();
        }
    }
}
