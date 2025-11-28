using Bookify.Application.DTOs;
using Bookify.Application.DTOs.Responses;
using Bookify.Application.DTOs.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookify.Application.Interfaces
{
    public interface IRoomService
    {
        Task<PagedResult<RoomDto>> SearchRoomsAsync(RoomSearchViewModel searchModel);
        Task<RoomDto?> GetRoomByIdAsync(int id);
        Task<IEnumerable<RoomTypeDto>> GetAllRoomTypesAsync();
        Task<IEnumerable<RoomDto>> GetFeaturedRoomsAsync(int count = 6);
    }
}
