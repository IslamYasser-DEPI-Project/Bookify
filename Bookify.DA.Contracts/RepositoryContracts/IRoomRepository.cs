using Bookify.DA.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookify.DA.Contracts.RepositoryContracts
{
    public interface IRoomRepository : IGenericRepository<Room>
    {
        Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, object? filter = null);
        Task<Room?> GetWithImagesAsync(int id);
        Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null);
        Task<IEnumerable<Room>> GetFeaturedRoomsAsync(int count);
    }
}