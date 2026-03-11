namespace backend.Services;

public class PreviewExecutionException : Exception
{
    public bool IsTimeout { get; }

    public PreviewExecutionException(string message, bool isTimeout, Exception? innerException = null)
        : base(message, innerException)
    {
        IsTimeout = isTimeout;
    }
}
