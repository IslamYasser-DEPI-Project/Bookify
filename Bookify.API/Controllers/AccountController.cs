using Bookify.Application.DTOs.Requests;
using Bookify.Application.DTOs.Responses;
using Bookify.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly UserManager<IdentityUser> _userManager;

    public AccountController(IAccountService accountService, UserManager<IdentityUser> userManager)
    {
        _accountService = accountService;
        _userManager = userManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        var (success, errors, message) = await _accountService.RegisterAsync(registerDto);
        if (!success)
            return BadRequest(new { errors });

        if (!string.IsNullOrWhiteSpace(message) && message.Equals("Waiting for approval", System.StringComparison.OrdinalIgnoreCase))
            return Accepted(new { message });

        return Ok(new { message = message ?? "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var (success, token, error) = await _accountService.LoginAsync(loginDto);
        if (!success)
            return BadRequest(new { error });

        
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        IEnumerable<string> roles = new List<string>();
        if (user != null)
        {
            roles = await _userManager.GetRolesAsync(user);
        }

        var userInfo = new UserInfoDTO
        {
            Id = user?.Id ?? string.Empty,
            UserName = user?.UserName ?? string.Empty,
            Email = user?.Email ?? string.Empty,
            Roles = roles,
            IsAdminRequestPending = false
        };
        return Ok(new { token, userInfo });
    }

}
