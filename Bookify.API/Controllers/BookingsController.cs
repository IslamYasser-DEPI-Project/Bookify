using Bookify.Application.DTOs.Responses;
using Bookify.Application.DTOs.ViewModels;
using Bookify.Application.Exceptions;
using Bookify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        private string? GetUserIdFromClaims()
        {
            var user = HttpContext?.User;
            if (user == null) return null;
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CartViewModel cart)
        {
            var userId = GetUserIdFromClaims();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            try
            {
                var booking = await _bookingService.CreateBookingAsync(userId, cart);
                return CreatedAtAction(nameof(GetByNumber), new { bookingNumber = booking.BookingNumber }, booking);
            }
            catch (BookingException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCart([FromBody] CartViewModel cart)
        {
            var ok = await _bookingService.ValidateCartAvailabilityAsync(cart);
            return Ok(new { available = ok });
        }

        [Authorize]
        [HttpGet("user")]
        public async Task<IActionResult> GetUserBookings()
        {
            var userId = GetUserIdFromClaims();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var bookings = await _bookingService.GetUserBookingsAsync(userId);
            return Ok(bookings);
        }

        [Authorize]
        [HttpGet("by-number/{bookingNumber}")]
        public async Task<IActionResult> GetByNumber(string bookingNumber)
        {
            var booking = await _bookingService.GetBookingByNumberAsync(bookingNumber);
            if (booking == null) return NotFound();
            return Ok(booking);
        }

        [Authorize]
        [HttpPost("{bookingId:int}/cancel")]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            var userId = GetUserIdFromClaims();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var success = await _bookingService.CancelBookingAsync(bookingId, userId);
            if (!success) return BadRequest(new { error = "Could not cancel booking (not found or not owned by user)." });
            return NoContent();
        }

        public class ConfirmPaymentRequest
        {
            public string BookingNumber { get; set; } = string.Empty;
            public string StripePaymentIntentId { get; set; } = string.Empty;
        }

        [Authorize]
        [HttpPost("confirm-payment")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.BookingNumber) || string.IsNullOrWhiteSpace(req.StripePaymentIntentId))
                return BadRequest(new { error = "BookingNumber and StripePaymentIntentId are required." });

            var ok = await _bookingService.ConfirmPaymentAsync(req.BookingNumber, req.StripePaymentIntentId);
            if (!ok) return BadRequest(new { error = "Could not confirm payment." });
            return Ok(new { success = true });
        }
    }
}
