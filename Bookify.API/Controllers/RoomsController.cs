using Bookify.Application.DTOs;
using Bookify.Application.DTOs.Responses;
using Bookify.Application.DTOs.ViewModels;
using Bookify.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet("search")]
        public async Task<ActionResult<PagedResult<RoomDto>>> Search([FromQuery] RoomSearchViewModel vm)
        {
            var result = await _roomService.SearchRoomsAsync(vm);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<RoomDto?>> GetById(int id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null) return NotFound();
            return Ok(room);
        }

        [HttpGet("types")]
        public async Task<ActionResult<System.Collections.Generic.IEnumerable<RoomTypeDto>>> GetTypes()
        {
            var types = await _roomService.GetAllRoomTypesAsync();
            return Ok(types);
        }

        [HttpGet("featured")]
        public async Task<ActionResult<System.Collections.Generic.IEnumerable<RoomDto>>> GetFeatured([FromQuery] int count = 6)
        {
            var items = await _roomService.GetFeaturedRoomsAsync(count);
            return Ok(items);
        }
    }
}
