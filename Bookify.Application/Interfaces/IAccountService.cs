using Bookify.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookify.Application.Interfaces
{
    public interface IAccountService
    {
        // Returns an optional message (e.g., "Waiting for approval"). Errors non-empty when Success == false.
        Task<(bool Success, IEnumerable<string> Errors, string Message)> RegisterAsync(RegisterDto registerDto);

        // Returns (Success, Token, Error). Token is non-null when Success == true.
        Task<(bool Success, string Token, string Error)> LoginAsync(LoginDto loginDto);
    }
}
