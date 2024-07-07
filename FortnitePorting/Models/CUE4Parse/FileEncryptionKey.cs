using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.Encryption.Aes;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Validators;

namespace FortnitePorting.Models.CUE4Parse;

public partial class FileEncryptionKey : ObservableValidator
{
    public static FileEncryptionKey Empty => new(Globals.ZERO_CHAR);
    
    [NotifyDataErrorInfo]
    [EncryptionKey]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EncryptionKey))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private string _keyString;

    public FileEncryptionKey(string keyString)
    {
        KeyString = keyString;
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(KeyString);
    public FAesKey EncryptionKey => new(KeyString);
}