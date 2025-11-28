using Avalonia.Controls;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Framework;
using AppWindowModel = FortnitePorting.WindowModels.AppWindowModel;

namespace FortnitePorting.Windows;

public partial class AppWindow : WindowBase<AppWindowModel>
{
    public AppWindow() : base(initializeWindowModel: false)
    {
        InitializeComponent();
        DataContext = WindowModel;
        
        Navigation.App.Initialize(Sidebar, ContentFrame);
        
        KeyDownEvent.AddClassHandler<TopLevel>((sender, args) => BlackHole.HandleKey(args.Key), handledEventsToo: true);
    }

    private void OnSidebarItemSelected(object? sender, SidebarItemSelectedArgs args)
    {
        if (!AppSettings.Installation.FinishedSetup) return;
        
        Navigation.App.Open(args.Tag);
    }
}