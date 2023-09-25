using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Application;
using FortnitePorting.Controls;
using FortnitePorting.Controls.Assets;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using AssetItem = FortnitePorting.Controls.Assets.AssetItem;

namespace FortnitePorting.Views;

public partial class ExportOptionsView : ViewBase<ExportOptionsViewModel>
{
    public ExportOptionsView()
    {
        InitializeComponent();
    }
}