using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StemCellsPro.Application.DTOs;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Shared.Responses;

namespace StemCellsPro.Api.Controllers;

public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ApiResponse<LoginResponseDto>("Login and Password are required."));
        }

        var response = await _authService.LoginAsync(request);
        return Ok(new ApiResponse<LoginResponseDto>(response, "Login successful"));

    }
}
