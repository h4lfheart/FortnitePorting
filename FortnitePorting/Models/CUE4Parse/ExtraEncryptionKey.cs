using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.Encryption.Aes;
using FortnitePorting.Shared.Validators;

namespace FortnitePorting.Models.CUE4Parse;

public partial class ExtraEncryptionKey : ObservableValidator
{
    [NotifyDataErrorInfo]
    [EncryptionKey]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EncryptionKey))]
    private string _keyString;

    public FAesKey EncryptionKey => new(KeyString);
}