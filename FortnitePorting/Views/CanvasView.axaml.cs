using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;
using Serilog;
using Poll = FortnitePorting.Models.Voting.Poll;
using PollItem = FortnitePorting.Models.Voting.PollItem;

namespace FortnitePorting.Views;

public partial class CanvasView : ViewBase<CanvasViewModel>
{
    public CanvasView() : base(CanvasVM)
    {
        InitializeComponent();

        ViewModel.BitmapImage = BitmapImage;
        
        KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDown, handledEventsToo: true);
    }
    
    private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Image image) return;

        var cursorPoint = e.GetCurrentPoint(image);
        var pixelPos = new Point((int) (cursorPoint.Position.X / (image.Bounds.Width / CanvasVM.Width)), 
            (int) (cursorPoint.Position.Y / (image.Bounds.Height / CanvasVM.Height)));
        
        var newPixel = new PlacePixel
        {
            X = (ushort) pixelPos.X,
            Y = (ushort) pixelPos.Y,
            R = ViewModel.Color.R,
            G = ViewModel.Color.G,
            B = ViewModel.Color.B,
        };
        
        await OnlineService.Send(new CanvasPixelPacket(newPixel));
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var cursorPoint = e.GetCurrentPoint(CanvasViewBox);
        var xSnap = CanvasViewBox.Bounds.Width / CanvasVM.Width;
        var ySnap = CanvasViewBox.Bounds.Width / CanvasVM.Height;

        var xPixel = (int) (cursorPoint.Position.X / xSnap);
        var yPixel = (int)(cursorPoint.Position.Y / ySnap);
        
        var pixelPos = new Point(xPixel * xSnap, yPixel * ySnap);
        
        if (ViewModel.ShowPixelAuthors 
            && ViewModel.PixelMetadata.TryGetValue(new Point(xPixel, yPixel), out var existingPixel)
            && !string.IsNullOrWhiteSpace(existingPixel.Name))
        {
            var popupParentPos = e.GetCurrentPoint(PopupParent);
            ViewModel.PopupName = existingPixel.Name;
            NamePopup.IsOpen = true;
            NamePopup.HorizontalOffset = popupParentPos.Position.X - PopupParent.Bounds.Width / 2;
            NamePopup.VerticalOffset = popupParentPos.Position.Y - PopupParent.Bounds.Height / 2 + 30;
        }
        else
        {
            NamePopup.IsOpen = false;
        }
        
        if (!ViewModel.ReadyToPlace)
        {
            HighlightCell.IsVisible = false;
            return;
        }

        HighlightCell.RenderTransform = new TranslateTransform(pixelPos.X - CanvasViewBox.Bounds.Width / 2 + xSnap / 2, 
            pixelPos.Y - CanvasViewBox.Bounds.Height / 2 + ySnap / 2);
        HighlightCell.Width = xSnap;
        HighlightCell.Height = ySnap;
    }
    
    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (!ViewModel.ReadyToPlace) return;
        
        HighlightCell.IsVisible = true;
    }
    
    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (!ViewModel.ReadyToPlace) return;
        
        HighlightCell.IsVisible = false;
        NamePopup.IsOpen = false;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) == 0) return;

        if (e.Key is Key.Add or Key.OemPlus)
        {
            CanvasViewBox.Width += 50;
            CanvasViewBox.Height += 50;
        }
        else if (e.Key is Key.Subtract or Key.OemMinus)
        {
            if (CanvasViewBox.Width <= 100 || CanvasViewBox.Height <= 50) return;
            
            CanvasViewBox.Width -= 50;
            CanvasViewBox.Height -= 50;
        }
    }

    private void ZoomOut(object? sender, RoutedEventArgs routedEventArgs)
    {
        if (CanvasViewBox.Width <= 100 || CanvasViewBox.Height <= 50) return;
        
        CanvasViewBox.Width -= 50;
        CanvasViewBox.Height -= 50;
    }

    private void ZoomIn(object? sender, RoutedEventArgs routedEventArgs)
    {
        CanvasViewBox.Width += 50;
        CanvasViewBox.Height += 50;
    }
}