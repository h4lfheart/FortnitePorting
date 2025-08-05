using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;
using FortnitePorting.Views.Setup;
using SkiaSharp;

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
