using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bookify.DA.Enums;

namespace Bookify.Application.DTOs.Requests
{
    public class CreateRoomRequest
    {
        public string? RoomNumber { get; set; }
        public int HotelId { get; set; }
        public int RoomTypeId { get; set; }
        public RoomStatus Status { get; set; } = RoomStatus.Available;
    }

    public class UpdateRoomRequest : CreateRoomRequest 
    {
        public string? NewRoomNumber { get; set; }
        public RoomStatus NewStatus { get; set; }
    }

    public class CreateRoomTypeRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal PricePerNight { get; set; }
        public int Capacity { get; set; }
    }

    public class UpdateRoomTypeRequest : CreateRoomTypeRequest { }
}
