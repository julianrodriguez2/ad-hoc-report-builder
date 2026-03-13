namespace backend.Services;

public class ReportExportException : Exception
{
    public bool IsTimeout { get; }

    public ReportExportException(string message, bool isTimeout, Exception? innerException = null)
        : base(message, innerException)
    {
        IsTimeout = isTimeout;
    }
}
