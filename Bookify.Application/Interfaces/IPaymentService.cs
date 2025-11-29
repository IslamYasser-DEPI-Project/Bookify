using Bookify.Application.DTOs.ViewModels;
using System.Threading.Tasks;

namespace Bookify.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionAsync(CartViewModel cart, string bookingNumber, string userId, string? customerEmail = null);
        Task<string> CreatePaymentIntentAsync(decimal amount, string currency, string bookingNumber);
        Task<bool> VerifyWebhookSignatureAsync(string payload, string signature);
        Task ProcessPaymentWebhookAsync(string payload);
    }
}
