using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.Application.DTOs.Responses
{
    public class RoomDto
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public int Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
