using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bookify.Application.DTOs.ViewModels;
using Bookify.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        
        [HttpPost("checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutSessionRequest request)
        {
            if (request == null || request.Cart == null)
                return BadRequest("Invalid request payload.");

            var url = await _paymentService.CreateCheckoutSessionAsync(request.Cart, request.BookingNumber, request.UserId, request.CustomerEmail);
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("Could not create checkout session.");

            return Ok(new { url });
        }

        
        [HttpPost("payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentIntentRequest request)
        {
            if (request == null || request.Amount <= 0)
                return BadRequest("Invalid request payload.");

            var clientSecret = await _paymentService.CreatePaymentIntentAsync(request.Amount, request.Currency, request.BookingNumber);
            if (string.IsNullOrWhiteSpace(clientSecret))
                return BadRequest("Could not create payment intent.");

            return Ok(new { clientSecret });
        }

        
        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            string payload;
            using (var reader = new StreamReader(Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;

            var valid = await _paymentService.VerifyWebhookSignatureAsync(payload, signature);
            if (!valid)
            {
                _logger.LogWarning("Invalid Stripe webhook signature.");
                return BadRequest();
            }

            await _paymentService.ProcessPaymentWebhookAsync(payload);
            return Ok();
        }

        
        public class CheckoutSessionRequest
        {
            public CartViewModel? Cart { get; set; }
            public string BookingNumber { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string? CustomerEmail { get; set; }
        }

        public class PaymentIntentRequest
        {
            public decimal Amount { get; set; }
            public string? Currency { get; set; }
            public string BookingNumber { get; set; } = string.Empty;
        }
    }
}
