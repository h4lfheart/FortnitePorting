using System.ComponentModel.DataAnnotations;
using System.IO;

namespace FortnitePorting.Validators;

public class DirectoryExistsAttribute(string FolderName) : ValidationAttribute
{
    
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var directory = value as string;
        if (!Directory.Exists(directory))
            return new ValidationResult($"{FolderName} must exist.");
        
        return ValidationResult.Success;
    }
}