using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace FortnitePorting.Validators;

public class ArchiveDirectoryAttribute(string? canValidateProperty = null) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(canValidateProperty))
        {
            var property = validationContext.ObjectType.GetProperty(canValidateProperty);
            var isEnabled = (bool?) property?.GetValue(validationContext.ObjectInstance) ?? false;
            if (!isEnabled) return ValidationResult.Success;
        }
        
        var directory = value as string;
        if (!Directory.Exists(directory))
            return new ValidationResult("Archive directory must exist.");

        var files = Directory.GetFiles(directory);
        if (!files.Any(file => file.Contains(".pak") || file.Contains(".sig") || file.Contains(".ucas") || file.Contains(".utoc")))
            return new ValidationResult("Archive directory must contain valid game files. (*.pak, *.sig, *.ucas, *.utoc)");
        
        return ValidationResult.Success;
    }
}