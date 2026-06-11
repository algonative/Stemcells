using StemCellsPro.Application.DTOs;

namespace StemCellsPro.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<bool> ValidateTokenAsync(string token);
    Task<string> GetDbConnectionStringAsync(string token);
}
