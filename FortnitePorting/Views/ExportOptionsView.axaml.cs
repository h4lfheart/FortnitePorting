using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ExportOptionsView : ViewBase<ExportOptionsViewModel>
{
    public ExportOptionsView() : base(AppSettings.Current.ExportOptions)
    {
        InitializeComponent();
    }
}