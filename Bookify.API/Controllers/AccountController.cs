using Bookify.Application.DTOs.Requests;
using Bookify.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        var (success, errors, message) = await _accountService.RegisterAsync(registerDto);
        if (!success)
            return BadRequest(new { errors });

        if (!string.IsNullOrWhiteSpace(message) && message.Equals("Waiting for approval", StringComparison.OrdinalIgnoreCase))
            return Accepted(new { message });

        return Ok(new { message = message ?? "Registration successful" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var (success, token, error) = await _accountService.LoginAsync(loginDto);
        if (!success)
            return BadRequest(new { error });
        return Ok(new { token });
    }

}
