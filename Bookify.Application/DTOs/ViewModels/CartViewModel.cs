using System.Collections.Generic;

namespace Bookify.Application.DTOs.ViewModels
{
    public class CartViewModel
    {
        public List<Bookify.Application.DTOs.ReservationCartItemDto> Items { get; set; } = new();
    }
}
