using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

public class CreateSavedReportDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string DefinitionJson { get; set; } = string.Empty;

    [Required]
    public string CreatedBy { get; set; } = string.Empty;
}
