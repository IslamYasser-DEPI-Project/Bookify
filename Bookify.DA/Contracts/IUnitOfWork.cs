using Bookify.DA.Contracts.RepositoryContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.DA.Contracts
{
    public interface IUnitOfWork
    {
        IBookingRepository BookingRepository { get; }
        ICustomerRepository CustomerRepository { get; }
        IHotelRepository HotelRepository { get; }
        IPaymentRepository PaymentRepository { get; }
        IPaymentTypeRepository PaymentTypeRepository { get; }
        IRoomRepository RoomRepository { get; }
        IRoomTypeRepository RoomTypeRepository { get; }

        IAdminApprovalRequest AdminApprovalRequestRepository { get; }


        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
