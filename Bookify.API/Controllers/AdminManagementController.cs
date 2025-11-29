using System;
using System.Linq;
using System.Threading.Tasks;
using Bookify.Application.DTOs.Responses;
using Bookify.DA.Contracts;
using Bookify.DA.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminManagementController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public AdminManagementController(IUnitOfWork uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        // DataTables-compatible list for rooms
        [HttpGet("rooms")]
        public async Task<IActionResult> GetRoomsForTable([FromQuery] int draw = 0, [FromQuery] int start = 0, [FromQuery] int length = 10, [FromQuery(Name = "search[value]")] string? search = null)
        {
            var query = _uow.RoomRepository.GetAllQueryable()
                .Include(r => r.RoomType)
                .Include(r => r.Hotel);

            var recordsTotal = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Room, Hotel>)query.Where(r =>
                    r.RoomNumber.Contains(s) ||
                    (r.Hotel != null && r.Hotel.Name.Contains(s)) ||
                    (r.RoomType != null && r.RoomType.Name.Contains(s)));
            }

            var recordsFiltered = await query.CountAsync();

            var items = await query
                .OrderBy(r => r.Id)
                .Skip(start)
                .Take(length)
                .Select(r => new
                {
                    r.Id,
                    r.RoomNumber,
                    HotelId = r.HotelID,
                    HotelName = r.Hotel != null ? r.Hotel.Name : string.Empty,
                    RoomTypeId = r.RoomTypeID,
                    RoomTypeName = r.RoomType != null ? r.RoomType.Name : string.Empty,
                    r.Status
                })
                .ToListAsync();

            return Ok(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data = items
            });
        }

        // Create room
        [HttpPost("rooms")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest dto)
        {
            if (dto == null) return BadRequest();

            var room = new Room
            {
                RoomNumber = dto.RoomNumber ?? string.Empty,
                HotelID = dto.HotelId,
                RoomTypeID = dto.RoomTypeId,
                Status = dto.Status
            };

            await _uow.RoomRepository.Add(room);
            await _uow.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRoomById), new { id = room.Id }, new { room.Id });
        }

        // Get single room for edit
        [HttpGet("rooms/{id}")]
        public async Task<IActionResult> GetRoomById(int id)
        {
            var room = await _uow.RoomRepository.GetById(id);
            if (room == null) return NotFound();

            return Ok(new
            {
                room.Id,
                room.RoomNumber,
                HotelId = room.HotelID,
                RoomTypeId = room.RoomTypeID,
                room.Status
            });
        }

        // Update room
        [HttpPut("rooms/{id}")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequest dto)
        {
            var existing = await _uow.RoomRepository.GetById(id);
            if (existing == null) return NotFound();

            existing.RoomNumber = dto.RoomNumber ?? existing.RoomNumber;
            existing.HotelID = dto.HotelId;
            existing.RoomTypeID = dto.RoomTypeId;
            existing.Status = dto.Status;

            await _uow.RoomRepository.Update(existing);
            await _uow.SaveChangesAsync();

            return NoContent();
        }

        // Delete room
        [HttpDelete("rooms/{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            await _uow.RoomRepository.Delete(id);
            await _uow.SaveChangesAsync();
            return NoContent();
        }

        // Room types list (DataTables)
        [HttpGet("room-types")]
        public async Task<IActionResult> GetRoomTypesForTable([FromQuery] int draw = 0, [FromQuery] int start = 0, [FromQuery] int length = 10, [FromQuery(Name = "search[value]")] string? search = null)
        {
            var query = _uow.RoomTypeRepository.GetAllQueryable()
                .Include(rt => rt.Rooms);

            var recordsTotal = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<RoomType, ICollection<Room>>)query.Where(rt => rt.Name.Contains(s) || rt.Description.Contains(s));
            }

            var recordsFiltered = await query.CountAsync();

            var items = await query
                .OrderBy(rt => rt.Id)
                .Skip(start)
                .Take(length)
                .Select(rt => new
                {
                    rt.Id,
                    rt.Name,
                    rt.Description,
                    rt.PricePerNight,
                    rt.Capacity,
                    RoomCount = rt.Rooms.Count()
                })
                .ToListAsync();

            return Ok(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data = items
            });
        }

        // Create room type
        [HttpPost("room-types")]
        public async Task<IActionResult> CreateRoomType([FromBody] CreateRoomTypeRequest dto)
        {
            if (dto == null) return BadRequest();

            var rt = new RoomType
            {
                Name = dto.Name ?? string.Empty,
                Description = dto.Description ?? string.Empty,
                PricePerNight = dto.PricePerNight,
                Capacity = dto.Capacity
            };

            await _uow.RoomTypeRepository.Add(rt);
            await _uow.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRoomTypeById), new { id = rt.Id }, new { rt.Id });
        }

        [HttpGet("room-types/{id}")]
        public async Task<IActionResult> GetRoomTypeById(int id)
        {
            var rt = await _uow.RoomTypeRepository.GetById(id);
            if (rt == null) return NotFound();

            return Ok(new
            {
                rt.Id,
                rt.Name,
                rt.Description,
                rt.PricePerNight,
                rt.Capacity
            });
        }

        [HttpPut("room-types/{id}")]
        public async Task<IActionResult> UpdateRoomType(int id, [FromBody] UpdateRoomTypeRequest dto)
        {
            var existing = await _uow.RoomTypeRepository.GetById(id);
            if (existing == null) return NotFound();

            existing.Name = dto.Name ?? existing.Name;
            existing.Description = dto.Description ?? existing.Description;
            existing.PricePerNight = dto.PricePerNight;
            existing.Capacity = dto.Capacity;

            await _uow.RoomTypeRepository.Update(existing);
            await _uow.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("room-types/{id}")]
        public async Task<IActionResult> DeleteRoomType(int id)
        {
            await _uow.RoomTypeRepository.Delete(id);
            await _uow.SaveChangesAsync();
            return NoContent();
        }

        // Bookings list (DataTables)
        [HttpGet("bookings")]
        public async Task<IActionResult> GetBookingsForTable([FromQuery] int draw = 0, [FromQuery] int start = 0, [FromQuery] int length = 10, [FromQuery(Name = "search[value]")] string? search = null)
        {
            var query = _uow.BookingRepository.GetAllQueryable()
                .Include(b => b.Room)
                .Include(b => b.Customer);

            var recordsTotal = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Booking, Customer>)query.Where(b =>
                    b.Id.ToString().Contains(s) ||
                    (b.Customer != null && b.Customer.Name.Contains(s)) ||
                    (b.Room != null && b.Room.RoomNumber.Contains(s)));
            }

            var recordsFiltered = await query.CountAsync();

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

            return Ok(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data = items
            });
        }

        // Local DTOs for admin endpoints
        public class CreateRoomRequest
        {
            public string? RoomNumber { get; set; }
            public int HotelId { get; set; }
            public int RoomTypeId { get; set; }
            public DA.Enums.RoomStatus Status { get; set; } = DA.Enums.RoomStatus.Available;
        }

        public class UpdateRoomRequest : CreateRoomRequest { }

        public class CreateRoomTypeRequest
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public decimal PricePerNight { get; set; }
            public int Capacity { get; set; }
        }

        public class UpdateRoomTypeRequest : CreateRoomTypeRequest { }
    }
}
