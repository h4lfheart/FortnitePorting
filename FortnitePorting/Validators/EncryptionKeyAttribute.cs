using System.ComponentModel.DataAnnotations;
using CUE4Parse.Utils;

namespace FortnitePorting.Validators;

public class EncryptionKeyAttribute : ValidationAttribute
{
    
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var key = value as string;
        if (!key.TryParseAesKey(out _))
            return new ValidationResult($"Invalid encryption key.");
        
        return ValidationResult.Success;
    }
}