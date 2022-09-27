namespace FortnitePorting.Views.Controls;

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

public class UniformGridPanel : VirtualizingPanel, IScrollInfo
{
    private Size _extent = new Size(0, 0);
    private Size _viewport = new Size(0, 0);
    private Point _offset = new Point(0, 0);
    private bool _canHorizontallyScroll = false;
    private bool _canVerticallyScroll = false;
    private ScrollViewer _owner;
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
        get { return (int)this.GetValue(ColumnsProperty); }
        set { this.SetValue(ColumnsProperty, value); }
    }

    /// <summary>
    /// Get/Set the amount of rows this grid should have
    /// </summary>
    public int Rows
    {
        get { return (int)this.GetValue(RowsProperty); }
        set { this.SetValue(RowsProperty, value); }
    }

    /// <summary>
    /// Get/Set the orientation of the panel
    /// </summary>
    public Orientation Orientation
    {
        get { return (Orientation)this.GetValue(OrientationProperty); }
        set { this.SetValue(OrientationProperty, value); }
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
        UIElementCollection children = this.InternalChildren;
        IItemContainerGenerator generator = this.ItemContainerGenerator;

        // Get the generator position of the first visible data item
        GeneratorPosition startPos = generator.GeneratorPositionFromIndex(firstVisibleItemIndex);

        // Get index where we'd insert the child for this position. If the item is realized
        // (position.Offset == 0), it's just position.Index, otherwise we have to add one to
        // insert after the corresponding child
        int childIndex = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1;

        using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
        {
            for (int itemIndex = firstVisibleItemIndex; itemIndex <= lastVisibleItemIndex; ++itemIndex, ++childIndex)
            {
                bool newlyRealized;

                // Get or create the child
                UIElement child = generator.GenerateNext(out newlyRealized) as UIElement;

                childIndex = Math.Max(0, childIndex);

                if (newlyRealized)
                {
                    // Figure out if we need to insert the child at the end or somewhere in the middle
                    if (childIndex >= children.Count)
                    {
                        base.AddInternalChild(child);
                    }
                    else
                    {
                        base.InsertInternalChild(childIndex, child);
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
        IItemContainerGenerator generator = this.ItemContainerGenerator;

        UpdateScrollInfo(finalSize);

        for (int i = 0; i < this.Children.Count; i++)
        {
            UIElement child = this.Children[i];

            int itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));

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
        UIElementCollection children = this.InternalChildren;
        IItemContainerGenerator generator = this.ItemContainerGenerator;

        for (int i = children.Count - 1; i >= 0; i--)
        {
            GeneratorPosition childGeneratorPos = new GeneratorPosition(i, 0);
            int itemIndex = generator.IndexFromGeneratorPosition(childGeneratorPos);
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
        Size childSize = GetChildSize(availableSize);

        if (this.Orientation == System.Windows.Controls.Orientation.Horizontal)
        {
            return new Size((this.Columns * childSize.Width) * Math.Ceiling((double)itemsCount / (this.Columns * this.Rows)), _viewport.Height);
        }
        else
        {
            var pageHeight = (this.Rows * childSize.Height);

            var sizeWidth = _viewport.Width;
            var sizeHeight = pageHeight * Math.Ceiling((double)itemsCount / (this.Rows * this.Columns));

            return new Size(sizeWidth, sizeHeight);
        }
    }


    /// <summary>
    /// Arrange the individual children
    /// </summary>
    /// <param name="index"></param>
    /// <param name="child"></param>
    /// <param name="finalSize"></param>
    private void ArrangeChild(int index, UIElement child, Size finalSize)
    {
        int row    = index / this.Columns;
        int column = index % this.Columns;

        double xPosition, yPosition;

        int currentPage;
        Size childSize = GetChildSize(finalSize);

        if (this.Orientation == System.Windows.Controls.Orientation.Horizontal)
        {
            currentPage = (int)Math.Floor((double)index / (this.Columns * this.Rows));

            xPosition = (currentPage * this._viewport.Width) + (column * childSize.Width);
            yPosition = (row % this.Rows) * childSize.Height;

            xPosition -= this._offset.X;
            yPosition -= this._offset.Y;
        }
        else
        {
            xPosition = (column * childSize.Width) - this._offset.X;
            yPosition = (row * childSize.Height)   - this._offset.Y;
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
        double width  = availableSize.Width / this.Columns;
        double height = availableSize.Height / this.Rows;

        return new Size(width, height);
    }

    /// <summary>
    /// Get the range of children that are visible
    /// </summary>
    /// <param name="firstVisibleItemIndex">The item index of the first visible item</param>
    /// <param name="lastVisibleItemIndex">The item index of the last visible item</param>
    private void GetVisibleRange(out int firstVisibleItemIndex, out int lastVisibleItemIndex)
    {
        Size childSize = GetChildSize(this._extent);

        int pageSize = this.Columns * this.Rows;
        int pageNumber = this.Orientation == System.Windows.Controls.Orientation.Horizontal ?
            (int)Math.Floor((double)this._offset.X / this._viewport.Width) :
            (int)Math.Floor((double)this._offset.Y / this._viewport.Height);

        firstVisibleItemIndex = (pageNumber * pageSize);
        lastVisibleItemIndex = firstVisibleItemIndex + (pageSize * 2) - 1;

        ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
        int itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;


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
        get { return _canHorizontallyScroll; }
        set { _canHorizontallyScroll = value; }
    }

    public bool CanVerticallyScroll
    {
        get { return _canVerticallyScroll; }
        set { _canVerticallyScroll = value; }
    }

    /// <summary>
    /// Get the extent height
    /// </summary>
    public double ExtentHeight
    {
        get { return this._extent.Height; }
    }

    /// <summary>
    /// Get the extent width
    /// </summary>
    public double ExtentWidth
    {
        get { return this._extent.Width; }
    }

    /// <summary>
    /// Get the current horizontal offset
    /// </summary>
    public double HorizontalOffset
    {
        get { return this._offset.X; }
    }

    /// <summary>
    /// Get the current vertical offset
    /// </summary>
    public double VerticalOffset
    {
        get { return this._offset.Y; }
    }

    /// <summary>
    /// Get/Set the scrollowner
    /// </summary>
    public System.Windows.Controls.ScrollViewer ScrollOwner
    {
        get { return this._owner; }
        set { this._owner = value; }
    }

    /// <summary>
    /// Get the Viewport Height
    /// </summary>
    public double ViewportHeight
    {
        get { return _viewport.Height; }
    }

    /// <summary>
    /// Get the Viewport Width
    /// </summary>
    public double ViewportWidth
    {
        get { return _viewport.Width; }
    }



    public void LineLeft()
    {
        this.SetHorizontalOffset(this._offset.X - _scrollLength);
    }

    public void LineRight()
    {
        this.SetHorizontalOffset(this._offset.X + _scrollLength);
    }

    public void LineUp()
    {
        this.SetVerticalOffset(this._offset.Y - _scrollLength);
    }
    public void LineDown()
    {
        this.SetVerticalOffset(this._offset.Y + _scrollLength);
    }

    public Rect MakeVisible(System.Windows.Media.Visual visual, Rect rectangle)
    {
        return new Rect();
    }

    public void MouseWheelDown()
    {
        if (this.Orientation == System.Windows.Controls.Orientation.Horizontal)
        {
            this.SetHorizontalOffset(this._offset.X + _scrollLength);
        }
        else
        {
            this.SetVerticalOffset(this._offset.Y + _scrollLength);
        }
    }

    public void MouseWheelUp()
    {
        if (this.Orientation == System.Windows.Controls.Orientation.Horizontal)
        {
            this.SetHorizontalOffset(this._offset.X - _scrollLength);
        }
        else
        {
            this.SetVerticalOffset(this._offset.Y - _scrollLength);
        }
    }

    public void MouseWheelLeft()
    {
        return;
    }

    public void MouseWheelRight()
    {
        return;
    }

    public void PageDown()
    {
        this.SetVerticalOffset(this._offset.Y + _viewport.Width);
    }

    public void PageUp()
    {
        this.SetVerticalOffset(this._offset.Y - _viewport.Width);
    }

    public void PageLeft()
    {
        this.SetHorizontalOffset(this._offset.X - _viewport.Width);
    }

    public void PageRight()
    {
        this.SetHorizontalOffset(this._offset.X + _viewport.Width);
    }


    public void SetHorizontalOffset(double offset)
    {
        _offset.X = Math.Max(0, offset);

        if (_owner != null)
        {
            _owner.InvalidateScrollInfo();
        }

        InvalidateMeasure();
    }

    public void SetVerticalOffset(double offset)
    {
        _offset.Y = Math.Max(0, offset);

        if (_owner != null)
        {
            _owner.InvalidateScrollInfo();
        }

        InvalidateMeasure();
    }

    private void UpdateScrollInfo(Size availableSize)
    {
        // See how many items there are
        ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
        int itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

        Size extent = MeasureExtent(availableSize, itemCount);
        // Update extent
        if (extent != _extent)
        {
            _extent = extent;
            if (_owner != null)
                _owner.InvalidateScrollInfo();
        }

        // Update viewport
        if (availableSize != _viewport)
        {
            _viewport = availableSize;
            if (_owner != null)
                _owner.InvalidateScrollInfo();
        }
    }

    #endregion IScrollInfo Implementation
}