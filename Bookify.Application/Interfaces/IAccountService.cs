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
        Task<(bool Success, IEnumerable<string> Errors)> RegisterAsync(RegisterDto registerDto);
        Task<(bool Success, string Error)> LoginAsync(LoginDto loginDto);

    }

}
