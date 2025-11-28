using System;

namespace Bookify.Application.DTOs.ViewModels
{
    public class RoomSearchViewModel
    {
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public int? RoomTypeId { get; set; }
        public string? Query { get; set; } //to get room number
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
