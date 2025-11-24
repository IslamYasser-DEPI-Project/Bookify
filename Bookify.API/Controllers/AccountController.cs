using Bookify.Application.DTOs;
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
        var (success, errors) = await _accountService.RegisterAsync(registerDto);
        if (!success)
            return BadRequest(new { errors }); // THIS returns the error array shown above
        return Ok("Registration successful");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var (success, error) = await _accountService.LoginAsync(loginDto);
        if (!success)
            return BadRequest(new { error }); 
        return Ok("Login successful");
    }

}
