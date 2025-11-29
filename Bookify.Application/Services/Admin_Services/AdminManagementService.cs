using System;
using System.Linq;
using System.Threading.Tasks;
using Bookify.Application.DTOs;
using Bookify.Application.DTOs.Requests;
using Bookify.Application.DTOs.Responses;
using Bookify.Application.Interfaces;
using Bookify.DA.Contracts;
using Bookify.DA.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Application.Services.Admin_Services
{
    public class AdminManagementService : IAdminManagementService
    {
        private readonly IUnitOfWork _uow;

        public AdminManagementService(IUnitOfWork uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<DataTableResult<RoomDto>> GetRoomsAsync(int draw, int start, int length, string? search)
        {
            var query = _uow.RoomRepository.GetAllQueryable()
                .Include(r => r.RoomType)
                .Include(r => r.Hotel);

            var total = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Room, Hotel>)query.Where(r =>
                    r.RoomNumber.Contains(s) ||
                    (r.Hotel != null && r.Hotel.Name.Contains(s)) ||
                    (r.RoomType != null && r.RoomType.Name.Contains(s)));
            }

            var filtered = await query.CountAsync();

            var items = await query
                .OrderBy(r => r.Id)
                .Skip(start)
                .Take(length)
                .Select(r => new RoomDto
                {
                    Id = r.Id,
                    RoomNumber = r.RoomNumber,
                    HotelId = r.HotelID,
                    HotelName = r.Hotel != null ? r.Hotel.Name : string.Empty,
                    RoomTypeId = r.RoomTypeID,
                    RoomTypeName = r.RoomType != null ? r.RoomType.Name : string.Empty,
                    PricePerNight = r.RoomType != null ? r.RoomType.PricePerNight : 0m,
                    Capacity = r.RoomType != null ? r.RoomType.Capacity : 0,
                    Status = r.Status.ToString()
                })
                .ToListAsync();

            return new DataTableResult<RoomDto>
            {
                Draw = draw,
                RecordsTotal = total,
                RecordsFiltered = filtered,
                Data = items
            };
        }

        public async Task<int> CreateRoomAsync(CreateRoomRequest dto)
        {
            var room = new Room
            {
                RoomNumber = dto.RoomNumber ?? string.Empty,
                HotelID = dto.HotelId,
                RoomTypeID = dto.RoomTypeId,
                Status = dto.Status
            };

            await _uow.RoomRepository.Add(room);
            await _uow.SaveChangesAsync();
            return room.Id;
        }

        public async Task<RoomDto?> GetRoomByIdAsync(int id)
        {
            var room = await _uow.RoomRepository.GetById(id);
            if (room == null) return null;

            var rt = await _uow.RoomTypeRepository.GetById(room.RoomTypeID);
            var hotel = await _uow.HotelRepository.GetById(room.HotelID);

            return new RoomDto
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                HotelId = room.HotelID,
                HotelName = hotel?.Name ?? string.Empty,
                RoomTypeId = room.RoomTypeID,
                RoomTypeName = rt?.Name ?? string.Empty,
                PricePerNight = rt?.PricePerNight ?? 0,
                Capacity = rt?.Capacity ?? 0,
                Status = room.Status.ToString()
            };
        }

        public async Task<bool> UpdateRoomAsync(int id, UpdateRoomRequest dto)
        {
            var existing = await _uow.RoomRepository.GetById(id);
            if (existing == null) return false;

            existing.RoomNumber = dto.RoomNumber ?? existing.RoomNumber;
            existing.HotelID = dto.HotelId;
            existing.RoomTypeID = dto.RoomTypeId;
            existing.Status = dto.Status;

            await _uow.RoomRepository.Update(existing);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRoomAsync(int id)
        {
            await _uow.RoomRepository.Delete(id);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<DataTableResult<RoomTypeDto>> GetRoomTypesAsync(int draw, int start, int length, string? search)
        {
            var query = _uow.RoomTypeRepository.GetAllQueryable()
                .Include(rt => rt.Rooms);

            var total = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<RoomType, ICollection<Room>>)query.Where(rt => rt.Name.Contains(s) || rt.Description.Contains(s));
            }

            var filtered = await query.CountAsync();

            var items = await query
                .OrderBy(rt => rt.Id)
                .Skip(start)
                .Take(length)
                .Select(rt => new RoomTypeDto
                {
                    Id = rt.Id,
                    Name = rt.Name,
                    Description = rt.Description,
                    PricePerNight = rt.PricePerNight,
                    Capacity = rt.Capacity,
                    RoomCount = rt.Rooms.Count()
                })
                .ToListAsync();

            return new DataTableResult<RoomTypeDto>
            {
                Draw = draw,
                RecordsTotal = total,
                RecordsFiltered = filtered,
                Data = items
            };
        }

        public async Task<int> CreateRoomTypeAsync(CreateRoomTypeRequest dto)
        {
            var rt = new RoomType
            {
                Name = dto.Name ?? string.Empty,
                Description = dto.Description ?? string.Empty,
                PricePerNight = dto.PricePerNight,
                Capacity = dto.Capacity
            };

            await _uow.RoomTypeRepository.Add(rt);
            await _uow.SaveChangesAsync();
            return rt.Id;
        }

        public async Task<RoomTypeDto?> GetRoomTypeByIdAsync(int id)
        {
            var rt = await _uow.RoomTypeRepository.GetById(id);
            if (rt == null) return null;

            return new RoomTypeDto
            {
                Id = rt.Id,
                Name = rt.Name,
                Description = rt.Description,
                PricePerNight = rt.PricePerNight,
                Capacity = rt.Capacity,
                RoomCount = (await _uow.RoomRepository.GetAllQueryable().CountAsync(r => r.RoomTypeID == rt.Id))
            };
        }

        public async Task<bool> UpdateRoomTypeAsync(int id, UpdateRoomTypeRequest dto)
        {
            var existing = await _uow.RoomTypeRepository.GetById(id);
            if (existing == null) return false;

            existing.Name = dto.Name ?? existing.Name;
            existing.Description = dto.Description ?? existing.Description;
            existing.PricePerNight = dto.PricePerNight;
            existing.Capacity = dto.Capacity;

            await _uow.RoomTypeRepository.Update(existing);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRoomTypeAsync(int id)
        {
            await _uow.RoomTypeRepository.Delete(id);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<DataTableResult<BookingDto>> GetBookingsAsync(int draw, int start, int length, string? search)
        {
            var query = _uow.BookingRepository.GetAllQueryable()
                .Include(b => b.Room)
                .Include(b => b.Customer);

            var total = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Booking, Customer>)query.Where(b =>
                    b.Id.ToString().Contains(s) ||
                    (b.Customer != null && b.Customer.Name.Contains(s)) ||
                    (b.Room != null && b.Room.RoomNumber.Contains(s)));
            }

            var filtered = await query.CountAsync();

            var items = await query
                .OrderByDescending(b => b.Id)
                .Skip(start)
                .Take(length)
                .Select(b => new BookingDto
                {
                    Id = b.Id,
                    CustomerId = b.CustomerID,
                    RoomId = b.RoomID,
                    RoomNumber = b.Room != null ? b.Room.RoomNumber : string.Empty,
                    BookingDate = b.BookingDate,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status.ToString()
                })
                .ToListAsync();

            return new DataTableResult<BookingDto>
            {
                Draw = draw,
                RecordsTotal = total,
                RecordsFiltered = filtered,
                Data = items
            };
        }
    }
}