using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Serilog;

namespace FortnitePorting.AppUtils;

public static class AppLog
{
    public static RichTextBox Logger;
    private static readonly BrushConverter BrushConverter = new();

    private static void Write(object text, string color = Globals.WHITE, FontWeight weights = default, bool newLine = true)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var document = Logger.Document;

            var textRange = new TextRange(document?.ContentEnd, document?.ContentEnd)
            {
                Text = text.ToString() + (newLine ? '\n' : "")
            };

            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, BrushConverter.ConvertFromString(color)!);
            textRange.ApplyPropertyValue(TextElement.FontWeightProperty, weights);

            Logger.ScrollToEnd();
        }, DispatcherPriority.Background);
    }
    
    public static void Information(string text)
    {
        Log.Information(text);
        Write("[INF] ", Globals.BLUE, FontWeights.Bold, false);
        Write(text);
    }
    
    public static void Warning(string text)
    {
        Log.Warning(text);
        Write("[WRN] ", Globals.YELLOW, FontWeights.Bold, false);
        Write(text);
    }
    
    public static void Error(string text)
    {
        Log.Error(text);
        Write("[ERR] ", Globals.RED, FontWeights.Bold, false);
        Write(text);
    }
}