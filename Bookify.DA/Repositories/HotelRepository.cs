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
    public class HotelRepository : GenericRepository<Hotel>, IHotelRepository
    {
        public HotelRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Hotel>> GetFeaturedHotelsAsync(int count)
        {
            if (count <= 0)
                return Array.Empty<Hotel>();

            return await GetAllQueryable()
                .Include(h => h.Rooms)
                .OrderBy(h => h.Id) 
                .Take(count)
                .ToListAsync();
        }

        public async Task<Hotel?> GetWithRoomsAsync(int hotelId)
        {
            return await GetAllQueryable()
                .Include(h => h.Rooms)
                    .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(h => h.Id == hotelId);
        }
    }
}
