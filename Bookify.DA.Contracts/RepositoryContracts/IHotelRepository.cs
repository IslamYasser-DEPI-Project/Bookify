using Bookify.DA.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookify.DA.Contracts.RepositoryContracts
{
    public interface IHotelRepository : IGenericRepository<Hotel>
    {
        Task<Hotel?> GetWithRoomsAsync(int hotelId);
        Task<IEnumerable<Hotel>> GetFeaturedHotelsAsync(int count);
    }
}