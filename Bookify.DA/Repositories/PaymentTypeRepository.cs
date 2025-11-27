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
    public class PaymentTypeRepository : GenericRepository<PaymentType>, IPaymentTypeRepository
    {
        public PaymentTypeRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PaymentType?> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            
            return await _context.PaymentTypes
                .FirstOrDefaultAsync(pt => pt.TypeName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
