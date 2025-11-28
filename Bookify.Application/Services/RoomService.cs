using Bookify.Application.DTOs;
using Bookify.Application.DTOs.Responses;
using Bookify.Application.DTOs.ViewModels;
using Bookify.Application.Interfaces;
using Bookify.DA.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bookify.Application.Services
{
    public class RoomService : IRoomService
    {
        private readonly IUnitOfWork _uow;

        public RoomService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<PagedResult<RoomDto>> SearchRoomsAsync(RoomSearchViewModel searchModel)
        {
            if (searchModel == null) throw new ArgumentNullException(nameof(searchModel));

            var checkIn = searchModel.CheckIn ?? DateTime.UtcNow;
            var checkOut = searchModel.CheckOut ?? checkIn.AddDays(1);

            // Use repository helper to filter by availability
            var roomsQuery = _uow.RoomRepository.GetAllQueryable()
                .Include(r => r.RoomType)
                .Include(r => r.Hotel)
                .Include(r => r.Bookings)
                .Where(r => r.Status.ToString() == "Available");

            // Remove rooms with overlapping bookings
            roomsQuery = roomsQuery.Where(r => !r.Bookings.Any(b =>
                b.Status.ToString() != "Cancelled"
                && b.CheckInDate < checkOut
                && b.CheckOutDate > checkIn));

            if (searchModel.RoomTypeId.HasValue)
                roomsQuery = roomsQuery.Where(r => r.RoomTypeID == searchModel.RoomTypeId.Value);

            if (!string.IsNullOrWhiteSpace(searchModel.Query))
            {
                var q = searchModel.Query.Trim();
                roomsQuery = roomsQuery.Where(r => r.RoomNumber.Contains(q));
            }

            var total = await roomsQuery.CountAsync();

            var page = Math.Max(1, searchModel.Page);
            var pageSize = Math.Max(1, searchModel.PageSize);

            var items = await roomsQuery
                .OrderBy(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return new PagedResult<RoomDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<RoomDto?> GetRoomByIdAsync(int id)
        {
            var room = await _uow.RoomRepository.GetWithImagesAsync(id);
            if (room == null) return null;

            return new RoomDto
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                HotelId = room.HotelID,
                HotelName = room.Hotel?.Name ?? string.Empty,
                RoomTypeId = room.RoomTypeID,
                RoomTypeName = room.RoomType?.Name ?? string.Empty,
                PricePerNight = room.RoomType?.PricePerNight ?? 0,
                Capacity = room.RoomType?.Capacity ?? 0,
                Status = room.Status.ToString()
            };
        }

        public async Task<System.Collections.Generic.IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync()
        {
            var types = await _uow.RoomTypeRepository.GetAllWithCountsAsync();
            return types.Select(t => new RoomTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                PricePerNight = t.PricePerNight,
                Capacity = t.Capacity,
                RoomCount = t.Rooms?.Count ?? 0
            });
        }

        public async Task<System.Collections.Generic.IEnumerable<RoomDto>> GetFeaturedRoomsAsync(int count = 6)
        {
            var rooms = await _uow.RoomRepository.GetFeaturedRoomsAsync(count);
            return rooms.Select(r => new RoomDto
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                HotelId = r.HotelID,
                HotelName = r.Hotel?.Name ?? string.Empty,
                RoomTypeId = r.RoomTypeID,
                RoomTypeName = r.RoomType?.Name ?? string.Empty,
                PricePerNight = r.RoomType?.PricePerNight ?? 0,
                Capacity = r.RoomType?.Capacity ?? 0,
                Status = r.Status.ToString()
            });
        }
    }
}
