using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FortnitePorting.Views.Controls;

public class UniformGridPanel : VirtualizingPanel, IScrollInfo
{
    private Size _extent = new(0, 0);
    private Size _viewport = new(0, 0);
    private Point _offset = new(0, 0);
    private bool _canHorizontallyScroll;
    private bool _canVerticallyScroll;
    private int _scrollLength = 25;

    //-----------------------------------------
    //
    // Dependency Properties
    //
    //-----------------------------------------

    #region Dependency Properties
    /// <summary>
    /// Columns DependencyProperty
    /// </summary>
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(int), typeof(UniformGridPanel),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>
    /// Rows DependencyProperty
    /// </summary>
    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register("Rows", typeof(int), typeof(UniformGridPanel),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>
    /// Orientation DependencyProperty
    /// </summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.RegisterAttached("Orientation", typeof(Orientation), typeof(UniformGridPanel),
        new FrameworkPropertyMetadata(Orientation.Vertical, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure));
    #endregion Dependency Properties

    //-----------------------------------------
    //
    // Public Properties
    //
    //-----------------------------------------

    #region Public Properties
    /// <summary>
    /// Get/Set the amount of columns this grid should have
    /// </summary>
    public int Columns
    {
        get => (int) GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>
    /// Get/Set the amount of rows this grid should have
    /// </summary>
    public int Rows
    {
        get => (int) GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    /// <summary>
    /// Get/Set the orientation of the panel
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation) GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }
    #endregion Public Properties

    //-----------------------------------------
    //
    // Overrides
    //
    //-----------------------------------------

    #region Overrides
    /// <summary>
    /// When items are removed, remove the corresponding UI if necessary
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
                RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                break;
        }
    }

    /// <summary>
    /// Measure the children
    /// </summary>
    /// <param name="availableSize">Size available</param>
    /// <returns>Size desired</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        UpdateScrollInfo(availableSize);

        int firstVisibleItemIndex, lastVisibleItemIndex;
        GetVisibleRange(out firstVisibleItemIndex, out lastVisibleItemIndex);

        // We need to access InternalChildren before the generator to work around a bug
        var children = InternalChildren;
        var generator = ItemContainerGenerator;

        // Get the generator position of the first visible data item
        var startPos = generator.GeneratorPositionFromIndex(firstVisibleItemIndex);

        // Get index where we'd insert the child for this position. If the item is realized
        // (position.Offset == 0), it's just position.Index, otherwise we have to add one to
        // insert after the corresponding child
        var childIndex = startPos.Offset == 0 ? startPos.Index : startPos.Index + 1;

        using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
        {
            for (var itemIndex = firstVisibleItemIndex; itemIndex <= lastVisibleItemIndex; ++itemIndex, ++childIndex)
            {
                // Get or create the child
                var child = generator.GenerateNext(out var newlyRealized) as UIElement;

                childIndex = Math.Max(0, childIndex);

                if (newlyRealized)
                {
                    // Figure out if we need to insert the child at the end or somewhere in the middle
                    if (childIndex >= children.Count)
                    {
                        AddInternalChild(child);
                    }
                    else
                    {
                        InsertInternalChild(childIndex, child);
                    }

                    generator.PrepareItemContainer(child);
                }
                else
                {
                    // The child has already been created, let's be sure it's in the right spot
                    Debug.Assert(child == children[childIndex], "Wrong child was generated");
                }

                // Measurements will depend on layout algorithm
                child.Measure(GetChildSize(availableSize));
            }
        }

        // Note: this could be deferred to idle time for efficiency
        CleanUpItems(firstVisibleItemIndex, lastVisibleItemIndex);

        if (availableSize.Height.Equals(double.PositiveInfinity))
        {
            Debug.WriteLine(_extent);
            return new Size(200, 200);
        }

        return availableSize;
    }

    /// <summary>
    /// Arrange the children
    /// </summary>
    /// <param name="finalSize">Size available</param>
    /// <returns>Size used</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var generator = ItemContainerGenerator;

        UpdateScrollInfo(finalSize);

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));

            ArrangeChild(itemIndex, child, finalSize);
        }

        return finalSize;
    }
    #endregion Overrides

    //-----------------------------------------
    //
    // Layout Specific Code
    //
    //-----------------------------------------

    #region Layout Specific Code
    /// <summary>
    /// Revisualizes items that are no longer visible
    /// </summary>
    /// <param name="minDesiredGenerated">first item index that should be visible</param>
    /// <param name="maxDesiredGenerated">last item index that should be visible</param>
    private void CleanUpItems(int minDesiredGenerated, int maxDesiredGenerated)
    {
        var children = InternalChildren;
        var generator = ItemContainerGenerator;

        for (var i = children.Count - 1; i >= 0; i--)
        {
            var childGeneratorPos = new GeneratorPosition(i, 0);
            var itemIndex = generator.IndexFromGeneratorPosition(childGeneratorPos);
            if (itemIndex < minDesiredGenerated || itemIndex > maxDesiredGenerated)
            {
                generator.Remove(childGeneratorPos, 1);
                RemoveInternalChildRange(i, 1);
            }
        }
    }

    /// <summary>
    /// Calculate the extent of the view based on the available size
    /// </summary>
    /// <param name="availableSize"></param>
    /// <returns></returns>
    private Size MeasureExtent(Size availableSize, int itemsCount)
    {
        var childSize = GetChildSize(availableSize);

        if (Orientation == Orientation.Horizontal)
        {
            return new Size(Columns * childSize.Width * Math.Ceiling((double) itemsCount / (Columns * Rows)), _viewport.Height);
        }
        var pageHeight = Rows * childSize.Height;

        var sizeWidth = _viewport.Width;
        var sizeHeight = pageHeight * Math.Ceiling((double) itemsCount / (Rows * Columns));

        return new Size(sizeWidth, sizeHeight);
    }

    /// <summary>
    /// Arrange the individual children
    /// </summary>
    /// <param name="index"></param>
    /// <param name="child"></param>
    /// <param name="finalSize"></param>
    private void ArrangeChild(int index, UIElement child, Size finalSize)
    {
        var row = index / Columns;
        var column = index % Columns;

        double xPosition, yPosition;

        var childSize = GetChildSize(finalSize);

        if (Orientation == Orientation.Horizontal)
        {
            var currentPage = (int) Math.Floor((double) index / (Columns * Rows));

            xPosition = currentPage * _viewport.Width + column * childSize.Width;
            yPosition = row % Rows * childSize.Height;

            xPosition -= _offset.X;
            yPosition -= _offset.Y;
        }
        else
        {
            xPosition = column * childSize.Width - _offset.X;
            yPosition = row * childSize.Height - _offset.Y;
        }

        child.Arrange(new Rect(xPosition, yPosition, childSize.Width, childSize.Height));
    }

    /// <summary>
    /// Get the size of the child element
    /// </summary>
    /// <param name="availableSize"></param>
    /// <returns>Returns the size of the child</returns>
    private Size GetChildSize(Size availableSize)
    {
        var width = availableSize.Width / Columns;
        var height = availableSize.Height / Rows;

        return new Size(width, height);
    }

    /// <summary>
    /// Get the range of children that are visible
    /// </summary>
    /// <param name="firstVisibleItemIndex">The item index of the first visible item</param>
    /// <param name="lastVisibleItemIndex">The item index of the last visible item</param>
    private void GetVisibleRange(out int firstVisibleItemIndex, out int lastVisibleItemIndex)
    {
        var childSize = GetChildSize(_extent);

        var pageSize = Columns * Rows;
        var pageNumber = Orientation == Orientation.Horizontal ?
            (int) Math.Floor(_offset.X / _viewport.Width) :
            (int) Math.Floor(_offset.Y / _viewport.Height);

        firstVisibleItemIndex = pageNumber * pageSize;
        lastVisibleItemIndex = firstVisibleItemIndex + pageSize * 2 - 1;

        var itemsControl = ItemsControl.GetItemsOwner(this);
        var itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

        if (lastVisibleItemIndex >= itemCount)
        {
            lastVisibleItemIndex = itemCount - 1;
        }
    }
    #endregion


    //-----------------------------------------
    //
    // IScrollInfo Implementation
    //
    //-----------------------------------------

    #region IScrollInfo Implementation
    public bool CanHorizontallyScroll
    {
        get => _canHorizontallyScroll;
        set => _canHorizontallyScroll = value;
    }

    public bool CanVerticallyScroll
    {
        get => _canVerticallyScroll;
        set => _canVerticallyScroll = value;
    }

    /// <summary>
    /// Get the extent height
    /// </summary>
    public double ExtentHeight => _extent.Height;

    /// <summary>
    /// Get the extent width
    /// </summary>
    public double ExtentWidth => _extent.Width;

    /// <summary>
    /// Get the current horizontal offset
    /// </summary>
    public double HorizontalOffset => _offset.X;

    /// <summary>
    /// Get the current vertical offset
    /// </summary>
    public double VerticalOffset => _offset.Y;

    /// <summary>
    /// Get/Set the scrollowner
    /// </summary>
    public ScrollViewer ScrollOwner { get; set; }

    /// <summary>
    /// Get the Viewport Height
    /// </summary>
    public double ViewportHeight => _viewport.Height;

    /// <summary>
    /// Get the Viewport Width
    /// </summary>
    public double ViewportWidth => _viewport.Width;

    public void LineLeft()
    {
        SetHorizontalOffset(_offset.X - _scrollLength);
    }

    public void LineRight()
    {
        SetHorizontalOffset(_offset.X + _scrollLength);
    }

    public void LineUp()
    {
        SetVerticalOffset(_offset.Y - _scrollLength);
    }
    public void LineDown()
    {
        SetVerticalOffset(_offset.Y + _scrollLength);
    }

    public Rect MakeVisible(Visual visual, Rect rectangle)
    {
        return new Rect();
    }

    public void MouseWheelDown()
    {
        if (Orientation == Orientation.Horizontal)
        {
            SetHorizontalOffset(_offset.X + _scrollLength);
        }
        else
        {
            SetVerticalOffset(_offset.Y + _scrollLength);
        }
    }

    public void MouseWheelUp()
    {
        if (Orientation == Orientation.Horizontal)
        {
            SetHorizontalOffset(_offset.X - _scrollLength);
        }
        else
        {
            SetVerticalOffset(_offset.Y - _scrollLength);
        }
    }

    public void MouseWheelLeft() { }

    public void MouseWheelRight() { }

    public void PageDown()
    {
        SetVerticalOffset(_offset.Y + _viewport.Width);
    }

    public void PageUp()
    {
        SetVerticalOffset(_offset.Y - _viewport.Width);
    }

    public void PageLeft()
    {
        SetHorizontalOffset(_offset.X - _viewport.Width);
    }

    public void PageRight()
    {
        SetHorizontalOffset(_offset.X + _viewport.Width);
    }

    public void SetHorizontalOffset(double offset)
    {
        _offset.X = Math.Max(0, offset);

        if (ScrollOwner != null)
        {
            ScrollOwner.InvalidateScrollInfo();
        }

        InvalidateMeasure();
    }

    public void SetVerticalOffset(double offset)
    {
        _offset.Y = Math.Max(0, offset);

        if (ScrollOwner != null)
        {
            ScrollOwner.InvalidateScrollInfo();
        }

        InvalidateMeasure();
    }

    private void UpdateScrollInfo(Size availableSize)
    {
        // See how many items there are
        var itemsControl = ItemsControl.GetItemsOwner(this);
        var itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

        var extent = MeasureExtent(availableSize, itemCount);
        // Update extent
        if (extent != _extent)
        {
            _extent = extent;
            if (ScrollOwner != null)
                ScrollOwner.InvalidateScrollInfo();
        }

        // Update viewport
        if (availableSize != _viewport)
        {
            _viewport = availableSize;
            ScrollOwner?.InvalidateScrollInfo();
        }
    }
    #endregion IScrollInfo Implementation
}