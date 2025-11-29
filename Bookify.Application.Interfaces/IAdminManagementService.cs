using System.Threading.Tasks;
using Bookify.Application.DTOs;
using Bookify.Application.DTOs.Requests;
using Bookify.Application.DTOs.Responses;

namespace Bookify.Application.Interfaces
{
    public interface IAdminManagementService
    {
        Task<DataTableResult<RoomDto>> GetRoomsAsync(int draw, int start, int length, string? search);
        Task<int> CreateRoomAsync(CreateRoomRequest dto);
        Task<RoomDto?> GetRoomByIdAsync(int id);
        Task<bool> UpdateRoomAsync(int id, UpdateRoomRequest dto);
        Task<bool> DeleteRoomAsync(int id);

        Task<DataTableResult<RoomTypeDto>> GetRoomTypesAsync(int draw, int start, int length, string? search);
        Task<int> CreateRoomTypeAsync(CreateRoomTypeRequest dto);
        Task<RoomTypeDto?> GetRoomTypeByIdAsync(int id);
        Task<bool> UpdateRoomTypeAsync(int id, UpdateRoomTypeRequest dto);
        Task<bool> DeleteRoomTypeAsync(int id);

        Task<DataTableResult<BookingDto>> GetBookingsAsync(int draw, int start, int length, string? search);
    }
}