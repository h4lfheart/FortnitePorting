using System.ComponentModel.DataAnnotations;

namespace FortnitePorting.Shared.Validators;

public class ArchiveDirectoryAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var directory = value as string;
        if (!Directory.Exists(directory))
            return new ValidationResult("Archive directory must exist.");

        var files = Directory.GetFiles(directory);
        if (!files.Any(file => file.Contains(".pak") || file.Contains(".sig") || file.Contains(".ucas") || file.Contains(".utoc")))
            return new ValidationResult("Archive directory must contain valid game files. (*.pak, *.sig, *.ucas, *.utoc)");
        
        return ValidationResult.Success;
    }
}