using Bookify.DA.Entities;
using System.Threading.Tasks;

namespace Bookify.DA.Contracts.RepositoryContracts
{
    public interface IPaymentTypeRepository : IGenericRepository<PaymentType>
    {
        Task<PaymentType?> GetByNameAsync(string name);
    }
}