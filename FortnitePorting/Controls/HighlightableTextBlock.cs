using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace FortnitePorting.Controls;

public class HighlightableTextBlock : TextBlock
{
    public static readonly StyledProperty<string> TextToHighlightProperty =
        AvaloniaProperty.Register<HighlightableTextBlock, string>(nameof(TextToHighlight), string.Empty);

    public static readonly StyledProperty<string> HighlightTextProperty =
        AvaloniaProperty.Register<HighlightableTextBlock, string>(nameof(HighlightText), string.Empty);

    public static readonly StyledProperty<bool> UseRegexProperty =
        AvaloniaProperty.Register<HighlightableTextBlock, bool>(nameof(UseRegex), false);

    public static readonly StyledProperty<IBrush?> HighlightBackgroundProperty =
        AvaloniaProperty.Register<HighlightableTextBlock, IBrush?>(
            nameof(HighlightBackground),
            new SolidColorBrush(Color.FromArgb(255, 255, 200, 0)));

    public static readonly StyledProperty<IBrush?> HighlightForegroundProperty =
        AvaloniaProperty.Register<HighlightableTextBlock, IBrush?>(
            nameof(HighlightForeground),
            Brushes.Black);

    public static readonly StyledProperty<CornerRadius> HighlightCornerRadiusProperty =
        AvaloniaProperty.Register<HighlightableTextBlock, CornerRadius>(
            nameof(HighlightCornerRadius),
            new CornerRadius(4));

    public string TextToHighlight
    {
        get => GetValue(TextToHighlightProperty);
        set => SetValue(TextToHighlightProperty, value);
    }

    public string HighlightText
    {
        get => GetValue(HighlightTextProperty);
        set => SetValue(HighlightTextProperty, value);
    }

    public bool UseRegex
    {
        get => GetValue(UseRegexProperty);
        set => SetValue(UseRegexProperty, value);
    }

    public IBrush? HighlightBackground
    {
        get => GetValue(HighlightBackgroundProperty);
        set => SetValue(HighlightBackgroundProperty, value);
    }

    public IBrush? HighlightForeground
    {
        get => GetValue(HighlightForegroundProperty);
        set => SetValue(HighlightForegroundProperty, value);
    }

    public CornerRadius HighlightCornerRadius
    {
        get => GetValue(HighlightCornerRadiusProperty);
        set => SetValue(HighlightCornerRadiusProperty, value);
    }

    static HighlightableTextBlock()
    {
        TextToHighlightProperty.Changed.AddClassHandler<HighlightableTextBlock>((tb, _) => tb.UpdateInlines());
        HighlightTextProperty.Changed.AddClassHandler<HighlightableTextBlock>((tb, _) => tb.UpdateInlines());
        UseRegexProperty.Changed.AddClassHandler<HighlightableTextBlock>((tb, _) => tb.UpdateInlines());
        HighlightBackgroundProperty.Changed.AddClassHandler<HighlightableTextBlock>((tb, _) => tb.UpdateInlines());
        HighlightForegroundProperty.Changed.AddClassHandler<HighlightableTextBlock>((tb, _) => tb.UpdateInlines());
        HighlightCornerRadiusProperty.Changed.AddClassHandler<HighlightableTextBlock>((tb, _) => tb.UpdateInlines());
    }

    private void UpdateInlines()
    {
        Inlines?.Clear();

        var text = TextToHighlight;
        var searchTerm = HighlightText;

        if (string.IsNullOrEmpty(text))
            return;

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            Inlines?.Add(new Run(text));
            return;
        }

        try
        {
            if (UseRegex)
            {
                HighlightWithRegex(text, searchTerm);
            }
            else
            {
                HighlightWithSimpleSearch(text, searchTerm);
            }
        }
        catch
        {
            Inlines?.Clear();
            Inlines?.Add(new Run(text));
        }
    }

    private void HighlightWithRegex(string text, string pattern)
    {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        var matches = regex.Matches(text);

        if (matches.Count == 0)
        {
            Inlines?.Add(new Run(text));
            return;
        }

        var lastIndex = 0;
        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                Inlines?.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
            }

            AddHighlightedText(match.Value);

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            Inlines?.Add(new Run(text[lastIndex..]));
        }
    }

    private void HighlightWithSimpleSearch(string text, string searchTerm)
    {
        var searchWords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (searchWords.Length == 0)
        {
            Inlines?.Add(new Run(text));
            return;
        }

        var matches = new List<(int Start, int Length)>();

        foreach (var word in searchWords)
        {
            var index = 0;
            while (index < text.Length)
            {
                var matchIndex = text.IndexOf(word, index, StringComparison.OrdinalIgnoreCase);
                if (matchIndex == -1) break;

                matches.Add((matchIndex, word.Length));
                index = matchIndex + 1;
            }
        }

        if (matches.Count == 0)
        {
            Inlines?.Add(new Run(text));
            return;
        }

        matches.Sort((a, b) => a.Start.CompareTo(b.Start));
        var mergedMatches = new List<(int Start, int End)>();

        foreach (var (start, length) in matches)
        {
            var end = start + length;

            if (mergedMatches.Count > 0)
            {
                var last = mergedMatches[^1];
                if (start <= last.End)
                {
                    mergedMatches[^1] = (last.Start, Math.Max(last.End, end));
                    continue;
                }
            }

            mergedMatches.Add((start, end));
        }

        var currentIndex = 0;
        foreach (var (start, end) in mergedMatches)
        {
            if (start > currentIndex)
            {
                Inlines?.Add(new Run(text.Substring(currentIndex, start - currentIndex)));
            }

            AddHighlightedText(text.Substring(start, end - start));

            currentIndex = end;
        }

        if (currentIndex < text.Length)
        {
            Inlines?.Add(new Run(text[currentIndex..]));
        }
    }

    private void AddHighlightedText(string text)
    {
        var border = new Border
        {
            Background = HighlightBackground,
            CornerRadius = HighlightCornerRadius,
            Padding = new Thickness(2, 0),
            Child = new TextBlock
            {
                Text = text,
                Foreground = HighlightForeground
            }
        };

        Inlines?.Add(new InlineUIContainer { Child = border });
    }
}