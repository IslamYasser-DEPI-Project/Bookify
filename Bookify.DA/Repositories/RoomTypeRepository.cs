using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bookify.DA.Contracts.RepositoryContracts;
using Bookify.DA.Data;
using Bookify.DA.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookify.DA.Repositories
{
    public class RoomTypeRepository : GenericRepository<RoomType>, IRoomTypeRepository
    {
        public RoomTypeRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<RoomType>> GetAllWithCountsAsync()
        {
            
            return await GetAllQueryable()
                .Include(rt => rt.Rooms)
                .ToListAsync();
        }

        public async Task<RoomType?> GetWithRoomsAsync(int id)
        {
            return await GetAllQueryable()
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.Id == id);
        }
    }
}
