namespace Amber.Application.Sync.Commands;

public class InsufficientStorageException : Exception
{
    public InsufficientStorageException() { }

    public InsufficientStorageException(string? message)
        : base(message) { }

    public InsufficientStorageException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
