using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;
using SkiaSharp;

namespace FortnitePorting.Windows;

public partial class TexturePreviewWindow : WindowBase<TexturePreviewViewModel>
{
    public TexturePreviewWindow(UTexture texture)
    {
        InitializeComponent();

        ViewModel.SetTexture(texture);
    }
    
    public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    public void OnMinimizeClicked(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    public void OnMaximizeClicked(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    public void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

}