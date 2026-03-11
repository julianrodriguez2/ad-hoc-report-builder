namespace backend.Services;

public class ReportValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public ReportValidationException(string error)
        : this([error])
    {
    }

    public ReportValidationException(IReadOnlyList<string> errors)
        : base("Report definition validation failed.")
    {
        Errors = errors;
    }
}
