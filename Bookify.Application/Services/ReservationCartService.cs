using Bookify.Application.DTOs;
using Bookify.Application.Helpers;
using Bookify.Application.Interfaces;
using Bookify.DA.Contracts;
using Bookify.DA.Entities;
using Bookify.DA.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Bookify.Application.Helpers;
using System.IdentityModel.Tokens.Jwt;

namespace Bookify.Application.Services
{
    public class ReservationCartService : IReservationCartService
    {
        private const string SessionKey = "ReservationCart";
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReservationCartService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public List<ReservationCartItemDto> GetCart()
        {
            return Session.GetObjectFromJson<List<ReservationCartItemDto>>(SessionKey) ?? new List<ReservationCartItemDto>();
        }

        public async Task<List<ReservationCartItemDto>> AddToCartAsync(ReservationCartItemDto item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.CheckOutDate <= item.CheckInDate)
                throw new ArgumentException("CheckOutDate must be after CheckInDate.");

            var room = await _unitOfWork.RoomRepository.GetById(item.RoomId);
            if (room == null)
                throw new InvalidOperationException("Room not found.");

            var items = GetCart();
            items.Add(item);
            Session.SetObjectAsJson(SessionKey, items);
            return items;
        }
        public Task<List<ReservationCartItemDto>> RemoveFromCartAsync(ReservationCartItemDto item)
        {
            var items = GetCart();
            var toRemove = items.FirstOrDefault(i => i.RoomId == item.RoomId && i.CheckInDate == item.CheckInDate && i.CheckOutDate == item.CheckOutDate);
            if (toRemove != null)
            {
                items.Remove(toRemove);
                Session.SetObjectAsJson(SessionKey, items);
            }
            return Task.FromResult(items);
        }

        public void ClearCart()
        {
            Session.Remove(SessionKey);
        }

        public async Task<(bool Success, string? Error, int BookedCount)> CheckoutAsync()
        {
            var items = GetCart();
            if (items == null || items.Count == 0)
                return (false, "Cart is empty.", 0);

            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
                return (false, "User is not authenticated.", 0);

            // Prefer ClaimTypes.NameIdentifier, fall back to JwtRegisteredClaimNames.Sub
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userId))
                return (false, "User id missing from token claims.", 0);

            var customer = await _unitOfWork.CustomerRepository
                .GetAllQueryable()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                return (false, "Customer profile not found for the authenticated user.", 0);

            var bookedCount = 0;

            foreach (var ci in items)
            {
                // Basic validation: room exists
                var room = await _unitOfWork.RoomRepository.GetById(ci.RoomId);
                if (room == null)
                    continue;

                var booking = new Booking
                {
                    CustomerID = customer.Id,
                    RoomID = ci.RoomId,
                    BookingDate = DateTime.UtcNow,
                    CheckInDate = ci.CheckInDate,
                    CheckOutDate = ci.CheckOutDate,
                    Status = BookingStatus.Pending
                };

                await _unitOfWork.BookingRepository.Add(booking);
                bookedCount++;
            }

            // Commit all bookings in a single Unit of Work transaction
            await _unitOfWork.SaveChangesAsync();

            // Clear session cart state (no tokens or credentials persisted)
            ClearCart();

            return (true, null, bookedCount);
        }



    }
}
