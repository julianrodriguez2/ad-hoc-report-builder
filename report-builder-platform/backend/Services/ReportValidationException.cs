namespace backend.Services;

public class ReportValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ReportValidationException(string error)
        : this([error])
    {
    }

    public ReportValidationException(IReadOnlyList<string> errors)
        : base("Report validation failed.")
    {
        Errors = errors;
    }
}
