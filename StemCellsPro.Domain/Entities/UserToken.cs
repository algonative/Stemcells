namespace StemCellsPro.Domain.Entities;

public class UserToken : BaseEntity
{
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string IpAddress { get; set; } = string.Empty;
}
