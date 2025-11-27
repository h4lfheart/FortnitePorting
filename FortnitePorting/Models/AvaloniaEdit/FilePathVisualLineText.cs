using System;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.Utils;
using FortnitePorting.Extensions;
using FortnitePorting.Windows;
using Newtonsoft.Json;

namespace FortnitePorting.Models.AvaloniaEdit;


public delegate void GamePathOnClick(string gamePath);

public class FilePathVisualLineText(string filePath, VisualLine parentVisualLine, int length) : VisualLineText(parentVisualLine, length)
{
    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var relativeOffset = startVisualColumn - VisualColumn;
        var remainingLength = DocumentLength - relativeOffset;

        if (remainingLength <= 0)
            return new TextEndOfParagraph();

        TextRunProperties.SetForegroundBrush(Brushes.Plum);

        return new TextCharacters(
            context.Document.GetText(context.VisualLine.FirstDocumentLine.Offset + RelativeTextOffset + relativeOffset, remainingLength),
            TextRunProperties);
    }

    private bool IsClickable() => !string.IsNullOrEmpty(filePath);

    protected override void OnQueryCursor(PointerEventArgs e)
    {
        if (e.Source is not TextView textView) return;
        
        if (IsClickable())
        {
            textView.Cursor = new Cursor(StandardCursorType.Hand);
            e.Handled = true;
        }
        else
        {
            textView.Cursor = null;
            e.Handled = false;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.Handled) return;
        if (!IsClickable()) return;
        if (e.GetCurrentPoint(null) is { Properties.IsLeftButtonPressed: false }) return;

        LoadFromPath(filePath);
        e.Handled = true;
    }

    protected override VisualLineText CreateInstance(int length)
    {
        var instance = new FilePathVisualLineText(filePath, ParentVisualLine, length);
        return instance;
    }

    private void LoadFromPath(string path)
    {
        var fullPath = UEParse.Provider.FixPath(path).SubstringBeforeLast(".");
        var package = UEParse.Provider.LoadPackage(fullPath);
        var exports = package.GetExports();
        PropertiesPreviewWindow.Preview(package.Name.SubstringAfterLast("/"), JsonConvert.SerializeObject(exports, Formatting.Indented));
    }
}