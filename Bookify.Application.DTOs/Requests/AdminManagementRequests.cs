namespace Bookify.Application.DTOs.Requests
{
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