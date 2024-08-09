using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.Encryption.Aes;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Validators;
using Newtonsoft.Json;

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

    [JsonIgnore] public bool IsEmpty => string.IsNullOrWhiteSpace(KeyString);
    [JsonIgnore] public FAesKey EncryptionKey => new(KeyString);
}