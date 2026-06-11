using System.Globalization;

namespace StemCellsPro.Shared.Exceptions;

public class AppException : Exception
{
    public AppException() : base() {}

    public AppException(string message) : base(message) {}

    public AppException(string message, params object[] args) 
        : base(String.Format(CultureInfo.CurrentCulture, message, args))
    {
    }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) {}
}
