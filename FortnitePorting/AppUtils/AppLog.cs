using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Serilog;
using Brush = System.Drawing.Brush;

namespace FortnitePorting.AppUtils;

public static class AppLog
{
    public static ItemsControl Logger;
    public static readonly BrushConverter BrushConverter = new();

    private static void Write(string specifier, string extra, string? specifierColor = Globals.BLUE, FontWeight specifierWeight = default)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var mainBlock = new StackPanel { Orientation = Orientation.Horizontal, Width = Logger.ActualWidth };

            var specifierBlock = new TextBlock
            {
                Text = specifier,
                FontWeight = specifierWeight,
                Foreground = (System.Windows.Media.Brush) BrushConverter.ConvertFromString(specifierColor!)!
            };

            mainBlock.Children.Add(specifierBlock);

            var extraBlock = new TextBlock();
            extraBlock.Text = extra;
            extraBlock.TextWrapping = TextWrapping.Wrap;
            extraBlock.SetResourceReference(Control.ForegroundProperty, AdonisUI.Brushes.ForegroundBrush);

            mainBlock.Children.Add(extraBlock);

            Logger.Items.Add(mainBlock);
        }, DispatcherPriority.Background);
    }

    public static void Information(string text)
    {
        Log.Information(text);
        Write("[INFO] ", text, Globals.BLUE, FontWeights.Bold);
    }

    public static void Warning(string text)
    {
        Log.Warning(text);
        Write("[WARN] ", text, Globals.YELLOW, FontWeights.Bold);
    }

    public static void Error(string text)
    {
        Log.Error(text);
        Write("[ERROR] ", text, Globals.RED, FontWeights.Bold);
    }
}