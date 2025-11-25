using Bookify.DA.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookify.DA.Contracts.RepositoryContracts
{
    public interface IRoomTypeRepository : IGenericRepository<RoomType>
    {
        Task<RoomType?> GetWithRoomsAsync(int id);
        Task<IEnumerable<RoomType>> GetAllWithCountsAsync();
    }
}