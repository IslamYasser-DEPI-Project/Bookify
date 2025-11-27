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
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Payment>> GetByBookingIdAsync(int bookingId)
        {
            return await _context.Payments
                .Include(p => p.PaymentType)
                .Where(p => p.BookingID == bookingId)
                .ToListAsync();
        }

        public async Task<Payment?> GetByPaymentNumberAsync(string paymentNumber)
        {
            if (string.IsNullOrWhiteSpace(paymentNumber))
                return null;

            
            if (int.TryParse(paymentNumber, out var id))
            {
                return await _context.Payments
                    .Include(p => p.PaymentType)
                    .Include(p => p.Booking)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }

            return null;
        }
    }
}
