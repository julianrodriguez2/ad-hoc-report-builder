namespace backend.DTOs;

public class LayoutSettingsDto
{
    public string TemplateId { get; set; } = string.Empty;

    public string ReportTitle { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string? LogoUrl { get; set; }

    public string? HeaderText { get; set; }

    public string? FooterText { get; set; }

    public string PageOrientation { get; set; } = "portrait";

    public string PageSize { get; set; } = "A4";

    public bool ShowGeneratedDate { get; set; } = true;

    public bool ShowPageNumbers { get; set; } = true;
}
