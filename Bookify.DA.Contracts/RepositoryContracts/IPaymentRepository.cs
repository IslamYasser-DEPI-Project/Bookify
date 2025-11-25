using Bookify.DA.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookify.DA.Contracts.RepositoryContracts
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<IEnumerable<Payment>> GetByBookingIdAsync(int bookingId);
        Task<Payment?> GetByPaymentNumberAsync(string paymentNumber);
    }
}