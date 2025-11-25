using Bookify.DA.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookify.DA.Contracts.RepositoryContracts
{
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        Task<Customer?> GetByUserIdAsync(string userId);
        Task<Customer?> GetWithBookingsAsync(int customerId);
        Task<IEnumerable<Customer>> GetCustomersWithRecentBookingsAsync(int days);
    }
}