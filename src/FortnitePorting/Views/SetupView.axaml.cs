using FortnitePorting.Framework;
using FortnitePorting.ViewModels;
using FortnitePorting.Views.Setup;

namespace FortnitePorting.Views;

public partial class SetupView : ViewBase<SetupViewModel>
{
    public SetupView()
    {
        InitializeComponent();
        DataContext = ViewModel;
        
        Navigation.Setup.Initialize(ContentFrame);
        Navigation.Setup.Open<WelcomeSetupView>();
    }
}
