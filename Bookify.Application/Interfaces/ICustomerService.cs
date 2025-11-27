using Bookify.Application.DTOs.Responses;
using System.Threading.Tasks;

namespace Bookify.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<CustomerDto?> GetByUserIdAsync(string userId);
        Task<CustomerDto> CreateForUserAsync(string userId, string email, string? name = null, string? phone = null);
    }
}
