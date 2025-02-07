using System.ComponentModel.DataAnnotations;

namespace Aevatar.AI.Options;

public class DeepSeekConfig
{
    public const string ConfigSectionName = "DeepSeek";

    [Required]
    public string Endpoint { get; set; } = string.Empty;
    
    [Required]
    public string ModelId { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}