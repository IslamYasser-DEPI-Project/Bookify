using Bookify.DA.Contracts;
using Bookify.DA.Contracts.RepositoryContracts;
using Bookify.DA.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.DA.Data
{
    internal class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;

        private readonly Lazy<IBookingRepository> _bookings;
        private readonly Lazy<ICustomerRepository> _customers;
        private readonly Lazy<IHotelRepository> _hotels;
        private readonly Lazy<IPaymentRepository> _payments;
        private readonly Lazy<IPaymentTypeRepository> _paymentTypes;
        private readonly Lazy<IRoomRepository> _rooms;
        private readonly Lazy<IRoomTypeRepository> _roomTypes;
        private readonly Lazy<IUserRepository> _users;

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
            _users = new Lazy<IUserRepository>(() => new UserRepository(_db));
        }


        public IBookingRepository Bookings => _bookings.Value;
        public ICustomerRepository Customers => _customers.Value;
        public IHotelRepository Hotels => _hotels.Value;
        public IPaymentRepository Payments => _payments.Value;
        public IPaymentTypeRepository PaymentTypes => _paymentTypes.Value;
        public IRoomRepository Rooms => _rooms.Value;
        public IRoomTypeRepository RoomTypes => _roomTypes.Value;
        public IUserRepository Users => _users.Value;

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }

    }
}
