using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Framework;
using FortnitePorting.Services;
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

        WindowModel.SupaBase.LevelUp += (sender, level) =>
        {
            TaskService.RunDispatcher(async () => await LevelUpOverlay.ShowLevelUp(level));
        };
        
        PointerMoved += OnPointerMoved;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        
        var pos = e.GetPosition(ChippyImage);
        var bounds = new Rect(ChippyImage.Bounds.Size);
        var isOver = bounds.Contains(pos);
        
        WindowModel.ChippyOpacity = isOver ? 0.0d : 1.0d;
        
    }

    private void OnSidebarItemSelected(object? sender, SidebarItemSelectedArgs args)
    {
        if (!AppSettings.Installation.FinishedSetup) return;
        
        Navigation.App.Open(args.Tag);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        App.Lifetime.Shutdown();
    }

}