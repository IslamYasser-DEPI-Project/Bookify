using Bookify.Application.DTOs;
using Bookify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationCartController : ControllerBase
    {
        private readonly IReservationCartService _cartService;

        public ReservationCartController(IReservationCartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet("GetCart")]
        public ActionResult<List<ReservationCartItemDto>> GetCart()
        {
            var cart = _cartService.GetCart();
            return Ok(cart);
        }

        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart([FromBody] ReservationCartItemDto item)
        {
            try
            {
                var cart = await _cartService.AddToCartAsync(item);
                return Ok(cart);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("RemoveFromCart")]
        public async Task<IActionResult> RemoveFromCart([FromBody] ReservationCartItemDto item)
        {
            var cart = await _cartService.RemoveFromCartAsync(item);
            return Ok(cart);
        }

        [HttpPost("ClearCart")]
        public IActionResult ClearCart()
        {
            _cartService.ClearCart();
            return NoContent();
        }

        [Authorize]
        [HttpPost("Checkout")]
        public async Task<IActionResult> Checkout()
        {
            var result = await _cartService.CheckoutAsync();
            if (!result.Success)
            {
                if (result.Error == "User is not authenticated." || result.Error == "User id missing from token claims.")
                    return Unauthorized(result.Error);

                if (result.Error == "Cart is empty." || result.Error == "Customer profile not found for the authenticated user.")
                    return BadRequest(result.Error);

                return StatusCode(500, result.Error); 
            }

            return Ok(new { booked = result.BookedCount });
        }
    }
}
