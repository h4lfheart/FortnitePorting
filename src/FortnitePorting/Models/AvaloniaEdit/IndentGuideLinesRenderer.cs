using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;

namespace FortnitePorting.Models.AvaloniaEdit;


public class IndentGuideLinesRenderer : IBackgroundRenderer
{
    private readonly TextEditor _editor;
    private static readonly Pen DefaultPen = new(new SolidColorBrush(Color.Parse("#212121")));
    private const int IndentSize = 2;

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
        if (textView.VisualLines.Count == 0)
            return;

        textView.EnsureVisualLines();

        var lineData = new List<(int lineNumber, int indentLevel, double topY, double bottomY)>();

        foreach (var visualLine in textView.VisualLines)
        {
            var line = _editor.Document.GetLineByNumber(visualLine.FirstDocumentLine.LineNumber);
            var text = _editor.Document.GetText(line);
            var indentation = GetLeadingSpaceCount(text);
            var indentLevel = indentation / IndentSize;

            var topY = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.LineTop) - _editor.VerticalOffset;
            var bottomY = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[^1], VisualYPosition.LineBottom) - _editor.VerticalOffset;

            lineData.Add((visualLine.FirstDocumentLine.LineNumber, indentLevel, topY, bottomY));
        }

        if (lineData.Count == 0)
            return;

        var maxIndentLevel = lineData.Max(d => d.indentLevel);

        for (var level = 2; level <= maxIndentLevel; level++)
        {
            double? segmentStart = null;
            double segmentEnd = 0;

            for (var i = 0; i < lineData.Count; i++)
            {
                var (lineNumber, indentLevel, topY, bottomY) = lineData[i];

                if (indentLevel >= level)
                {
                    segmentStart ??= topY;
                    segmentEnd = bottomY;

                    var isLastLine = i == lineData.Count - 1;
                    var nextLineBreaks = !isLastLine && lineData[i + 1].indentLevel < level;

                    if (!isLastLine && !nextLineBreaks) continue;
                    
                    var xPosition = GetXPositionForIndentLevel(textView, lineData[i].lineNumber, level);
                    drawingContext.DrawLine(DefaultPen, 
                        new Point(xPosition, segmentStart.Value), 
                        new Point(xPosition, segmentEnd));
                    segmentStart = null;
                }
                else
                {
                    if (segmentStart is null) continue;
                    var xPosition = GetXPositionForIndentLevel(textView, lineData[i - 1].lineNumber, level);
                    drawingContext.DrawLine(DefaultPen, 
                        new Point(xPosition, segmentStart.Value), 
                        new Point(xPosition, segmentEnd));
                    segmentStart = null;
                }
            }
        }
    }

    private double GetXPositionForIndentLevel(TextView textView, int lineNumber, int indentLevel)
    {
        var position = textView.GetVisualPosition(
            new TextViewPosition(lineNumber, indentLevel * IndentSize), 
            VisualYPosition.TextTop);
        return position.X - 5;
    }

    private int GetLeadingSpaceCount(string text)
    {
        var count = 0;
        foreach (var ch in text)
        {
            if (ch == ' ')
                count++;
            else if (ch == '\t')
                count += IndentSize;
            else
                break;
        }
        return count;
    }
}