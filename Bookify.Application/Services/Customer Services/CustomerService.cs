using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bookify.Application.DTOs.Responses;
using Bookify.Application.Exceptions;
using Bookify.Application.Interfaces;
using Bookify.DA.Contracts;
using Bookify.DA.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CustomerDto?> GetByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var customer = await _unitOfWork.CustomerRepository
                .GetAllQueryable()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                return null;

            return Map(customer);
        }

        public async Task<CustomerDto> CreateForUserAsync(string userId, string email, string? name = null, string? phone = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId is required.", nameof(userId));

            var existing = await _unitOfWork.CustomerRepository
                .GetAllQueryable()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (existing != null)
                return Map(existing);

            var customer = new Customer
            {
                UserId = userId,
                Email = email ?? string.Empty,
                Name = name ?? email ?? string.Empty,
                Phone = phone ?? string.Empty
            };

            await _unitOfWork.CustomerRepository.Add(customer);
            await _unitOfWork.SaveChangesAsync();

            return Map(customer);
        }

        private static CustomerDto Map(Customer c) =>
            new CustomerDto
            {
                Id = c.Id,
                UserId = c.UserId,
                Email = c.Email,
                Name = c.Name,
                Phone = c.Phone
            };
    }
}
