using FortnitePorting.Shared.Extensions;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace FortnitePorting.Controls;

public class TransformUpdater
{
    private readonly Control _control;
    private readonly Control _otherControl;
    private readonly Point _offset;
    
    private Point _originalOtherControlPosition;

    public TransformUpdater(Control control, Control otherControl, Point offset)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
        _otherControl = otherControl ?? throw new ArgumentNullException(nameof(otherControl));
        _offset = offset;
        
        var otherBounds = otherControl.GetTransformedBounds()!.Value.Transform;
        _originalOtherControlPosition = new Point(otherBounds.OffsetX(), otherBounds.OffsetY());

    }

    public void UpdateTransform()
    {
        var matrix = _control.GetTransformedBounds()!.Value.Transform;
        var deltaX = _originalOtherControlPosition.X - matrix.OffsetX();
        var deltaY = _originalOtherControlPosition.Y - matrix.OffsetY();

        _otherControl.RenderTransform = new TranslateTransform(-deltaX + _offset.X, -deltaY + _offset.Y);
    }
}
