using System.ComponentModel.DataAnnotations;
using CUE4Parse.Utils;
using FortnitePorting.Models.CUE4Parse;

namespace FortnitePorting.Validators;

public class EncryptionKeyAttribute(string? canValidateProperty = null) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(canValidateProperty))
        {
            var property = validationContext.ObjectType.GetProperty(canValidateProperty);
            var isEnabled = (bool?) property?.GetValue(validationContext.ObjectInstance) ?? false;
            if (!isEnabled) return ValidationResult.Success;
        }

        var targetKey = value switch
        {
            FileEncryptionKey encryptionKey => encryptionKey.KeyString,
            string stringKey => stringKey,
            _ => null
        };
        
        if (targetKey is null || !targetKey.TryParseAesKey(out _))
            return new ValidationResult($"Invalid encryption key.");
        
        return ValidationResult.Success;
    }
}