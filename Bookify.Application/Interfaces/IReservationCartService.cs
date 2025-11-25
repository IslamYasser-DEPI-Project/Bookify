using Bookify.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.Application.Interfaces
{
    public interface IReservationCartService
    {
        List<ReservationCartItemDto> GetCart();
        Task<List<ReservationCartItemDto>> AddToCartAsync(ReservationCartItemDto item);
        Task<List<ReservationCartItemDto>> RemoveFromCartAsync(ReservationCartItemDto item);
        
        Task<(bool Success, string? Error, int BookedCount)> CheckoutAsync();

        void ClearCart();
    }

}
