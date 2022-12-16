namespace Bootloader.Domain.Exceptions
{
    public class AlreadyUpToDateException : Exception
    {
        public AlreadyUpToDateException()
        {
        }

        public AlreadyUpToDateException(string? message) : base(message)
        {
        }

        public AlreadyUpToDateException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
