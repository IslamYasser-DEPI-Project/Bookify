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
    public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Customer?> GetByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await GetAllQueryable()
                .Include(c => c.Bookings)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<IEnumerable<Customer>> GetCustomersWithRecentBookingsAsync(int days)
        {
            if (days <= 0)
                return Array.Empty<Customer>();

            var cutoff = DateTime.UtcNow.AddDays(-days);

            return await GetAllQueryable()
                .Include(c => c.Bookings)
                .Where(c => c.Bookings.Any(b => b.BookingDate >= cutoff))
                .ToListAsync();
        }

        public async Task<Customer?> GetWithBookingsAsync(int customerId)
        {
            return await GetAllQueryable()
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.Room)
                .FirstOrDefaultAsync(c => c.Id == customerId);
        }
    }
}
