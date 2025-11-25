using Bookify.DA.Contracts;
using Bookify.DA.Contracts.RepositoryContracts;
using Bookify.DA.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.DA.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;

        private readonly Lazy<IBookingRepository> _bookings;
        private readonly Lazy<ICustomerRepository> _customers;
        private readonly Lazy<IHotelRepository> _hotels;
        private readonly Lazy<IPaymentRepository> _payments;
        private readonly Lazy<IPaymentTypeRepository> _paymentTypes;
        private readonly Lazy<IRoomRepository> _rooms;
        private readonly Lazy<IRoomTypeRepository> _roomTypes;
        private readonly Lazy<IAdminApprovalRequest> _adminApprovalRequests;


        public UnitOfWork(AppDbContext db)
        {
            _db = db;
            _bookings = new Lazy<IBookingRepository>(() => new BookingRepository(_db));
            _customers = new Lazy<ICustomerRepository>(() => new CustomerRepository(_db));
            _hotels = new Lazy<IHotelRepository>(() => new HotelRepository(_db));
            _payments = new Lazy<IPaymentRepository>(() => new PaymentRepository(_db));
            _paymentTypes = new Lazy<IPaymentTypeRepository>(() => new PaymentTypeRepository(_db));
            _rooms = new Lazy<IRoomRepository>(() => new RoomRepository(_db));
            _roomTypes = new Lazy<IRoomTypeRepository>(() => new RoomTypeRepository(_db));
            _adminApprovalRequests = new Lazy<IAdminApprovalRequest>(() => new AdminApprovalRequestRepository(_db));


        }


        public IBookingRepository BookingRepository => _bookings.Value;
        public ICustomerRepository CustomerRepository => _customers.Value;
        public IHotelRepository HotelRepository => _hotels.Value;
        public IPaymentRepository PaymentRepository => _payments.Value;
        public IPaymentTypeRepository PaymentTypeRepository => _paymentTypes.Value;
        public IRoomRepository RoomRepository => _rooms.Value;
        public IRoomTypeRepository RoomTypeRepository => _roomTypes.Value;
        public IAdminApprovalRequest AdminApprovalRequestRepository => _adminApprovalRequests.Value;
        

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }

    }
}
