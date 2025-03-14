using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace FortnitePorting.Shared.Extensions;

public class StringToDocumentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text) return null;

        return new TextDocument(text);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TextDocument document) return null;

        return document.Text;
    }
}

public class IndentGuideLinesRenderer : IBackgroundRenderer
{
    private TextEditor _editor;

    private static readonly Pen _defaultPen = new(new SolidColorBrush(Color.Parse("#40bdbdbd")));

    private const int INDENT_COUNT = 2;

    public IndentGuideLinesRenderer(TextEditor editor)
    {
        _editor = editor;

        var scrollViewer = editor.GetVisualDescendants().OfType<ScrollViewer>().First();
        scrollViewer.ScrollChanged += (sender, args) =>
        {
            _editor.TextArea.TextView.InvalidateVisual();
        };
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        textView.EnsureVisualLines();

        foreach (var visualLine in textView.VisualLines)
        {
            var line = _editor.Document.GetLineByNumber(visualLine.FirstDocumentLine.LineNumber);
            var text = _editor.Document.GetText(line);
            var indentation = 0;

            foreach (var character in text)
            {
                if (character != ' ') break;
                
                indentation++;

                if (indentation <= INDENT_COUNT || (indentation - INDENT_COUNT) % INDENT_COUNT != 0) continue;
                
                var startX = textView.GetVisualPosition(new TextViewPosition(line.LineNumber, indentation), VisualYPosition.TextTop).X - 5;
                var startY = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.LineTop) - _editor.VerticalOffset;
                var endY = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.LineBottom) - _editor.VerticalOffset;


                drawingContext.DrawLine(_defaultPen, new Point(startX, startY), new Point(startX, endY));
            }
        }
    }
}

