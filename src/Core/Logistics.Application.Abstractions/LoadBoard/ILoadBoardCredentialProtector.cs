namespace Logistics.Application.Abstractions.LoadBoard;

public interface ILoadBoardCredentialProtector
{
    string? Protect(string? value);
    string? Unprotect(string? value);
}
