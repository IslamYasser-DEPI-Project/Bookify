using System;

namespace Bookify.Application.DTOs.Responses
{
    public class BookingDto
    {
        public int Id { get; set; }
        public string BookingNumber { get; set; } = string.Empty; // fallback to Id.ToString() when not present
        public int CustomerId { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
