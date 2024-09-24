using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace FortnitePorting.Controls.WrapPanel;

public class AlignableWrapPanel : Panel
{
    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty = AvaloniaProperty.Register<AlignableWrapPanel, HorizontalAlignment>(nameof(HorizontalContentAlignment), HorizontalAlignment.Left);

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    
    protected override Size MeasureOverride(Size constraint)
    {
        var curLineSize = new Size();
        var panelSize = new Size();

        var children = VisualChildren;

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i] as Control;

            // Flow passes its own constraint to children
            child.Measure(constraint);
            Size sz = child.DesiredSize;

            if (curLineSize.Width + sz.Width > constraint.Width) //need to switch to another line
            {
                panelSize = panelSize.WithWidth(Math.Max(curLineSize.Width, panelSize.Width)).WithHeight(panelSize.Height + curLineSize.Height);
                curLineSize = sz;

                if (sz.Width > constraint.Width) // if the element is wider then the constraint - give it a separate line                    
                {
                    panelSize = panelSize.WithWidth(Math.Max(sz.Width, panelSize.Width)).WithHeight(panelSize.Height + sz.Height);
                    curLineSize = new Size();
                }
            }
            else //continue to accumulate a line
            {
                curLineSize = curLineSize.WithWidth(curLineSize.Width + sz.Width)
                    .WithHeight(Math.Max(sz.Height, curLineSize.Height));
            }
        }

        // the last line size, if any need to be added
        panelSize = panelSize.WithWidth(Math.Max(curLineSize.Width, panelSize.Width))
            .WithHeight(panelSize.Height + curLineSize.Height);

        return panelSize;
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        var firstInLine = 0;
        var curLineSize = new Size();
        double accumulatedHeight = 0;
        var children = VisualChildren;

        for (var i = 0; i < children.Count; i++)
        {
            Size sz = (children[i] as Control).Bounds.Size;

            if (curLineSize.Width + sz.Width > arrangeBounds.Width) //need to switch to another line
            {
                ArrangeLine(accumulatedHeight, curLineSize, arrangeBounds.Width, firstInLine, i);

                accumulatedHeight += curLineSize.Height;
                curLineSize = sz;

                if (sz.Width > arrangeBounds.Width) //the element is wider then the constraint - give it a separate line                    
                {
                    ArrangeLine(accumulatedHeight, sz, arrangeBounds.Width, i, ++i);
                    accumulatedHeight += sz.Height;
                    curLineSize = new Size();
                }
                firstInLine = i;
            }
            else //continue to accumulate a line
            {
                curLineSize = curLineSize.WithWidth(curLineSize.Width + sz.Width)
                    .WithHeight(Math.Max(sz.Height, curLineSize.Height));
            }
        }

        if (firstInLine < children.Count)
            ArrangeLine(accumulatedHeight, curLineSize, arrangeBounds.Width, firstInLine, children.Count);

        return arrangeBounds;
    }

    private void ArrangeLine(double y, Size lineSize, double boundsWidth, int start, int end)
    {
        var x = HorizontalContentAlignment switch
        {
            HorizontalAlignment.Center => (boundsWidth - lineSize.Width) / 2,
            HorizontalAlignment.Right => boundsWidth - lineSize.Width,
            _ => 0
        };
        
        foreach (var child in VisualChildren.OfType<Control>().ToArray()[start..end])
        {
            child.Arrange(new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height));
            x += child.DesiredSize.Width;
        }
        
        InvalidateArrange();
    }
}