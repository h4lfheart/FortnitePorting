using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using AsyncImageLoader;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Services;

namespace FortnitePorting.Controls.Chat;

public class ChatMentionTextBlock : TextBlock
{
    public static readonly StyledProperty<string> TextToHighlightProperty =
        AvaloniaProperty.Register<ChatMentionTextBlock, string>(nameof(TextToHighlight), string.Empty);

    public static readonly StyledProperty<IBrush?> HighlightProperty =
        AvaloniaProperty.Register<ChatMentionTextBlock, IBrush?>(
            nameof(CurrentUserHighlight),
            new SolidColorBrush(Color.FromArgb(169, 149, 59, 248)));

    public static readonly StyledProperty<IBrush?> HighlightForegroundProperty =
        AvaloniaProperty.Register<ChatMentionTextBlock, IBrush?>(
            nameof(HighlightForeground),
            Brushes.White);

    public static readonly StyledProperty<CornerRadius> HighlightCornerRadiusProperty =
        AvaloniaProperty.Register<ChatMentionTextBlock, CornerRadius>(
            nameof(HighlightCornerRadius),
            new CornerRadius(4));
    public string TextToHighlight
    {
        get => GetValue(TextToHighlightProperty);
        set => SetValue(TextToHighlightProperty, value);
    }

    public IBrush? CurrentUserHighlight
    {
        get => GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
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

    static ChatMentionTextBlock()
    {
        TextToHighlightProperty.Changed.AddClassHandler<ChatMentionTextBlock>((tb, _) => tb.UpdateInlines());
        HighlightProperty.Changed.AddClassHandler<ChatMentionTextBlock>((tb, _) => tb.UpdateInlines());
        HighlightForegroundProperty.Changed.AddClassHandler<ChatMentionTextBlock>((tb, _) => tb.UpdateInlines());
        HighlightCornerRadiusProperty.Changed.AddClassHandler<ChatMentionTextBlock>((tb, _) => tb.UpdateInlines());
    }

    private void UpdateInlines()
    {
        Inlines?.Clear();

        var text = TextToHighlight;

        if (string.IsNullOrEmpty(text))
            return;

        try
        {
            HighlightMentions(text);
        }
        catch
        {
            Inlines?.Clear();
            Inlines?.Add(new Run(text));
        }
    }

    private void HighlightMentions(string text)
    {
        var mentionPattern = @"<@([a-f0-9\-]+|everyone)>";
        var regex = new Regex(mentionPattern, RegexOptions.IgnoreCase);
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

            var userId = match.Groups[1].Value;
            AddClickableMention(userId);

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            Inlines?.Add(new Run(text[lastIndex..]));
        }
    }

    private void AddClickableMention(string userId)
    {
        var isEveryone = userId.Equals("everyone", StringComparison.OrdinalIgnoreCase);
        var textBlock = new TextBlock
        {
            Text = $"@{userId}",
            Foreground = HighlightForeground
        };

        var border = new Border
        {
            Background = CurrentUserHighlight,
            CornerRadius = HighlightCornerRadius,
            Padding = new Thickness(2, 0),
            Child = textBlock,
            Cursor = new Cursor(isEveryone ? StandardCursorType.Arrow : StandardCursorType.Hand)
        };

        if (!isEveryone)
        {
            TaskService.Run(async () =>
            {
                await TaskService.RunDispatcherAsync(async () =>
                {
                    if (await AppServices.Chat.GetUser(userId) is { } user)
                        textBlock.Text = $"@{user.DisplayName}";
                });
            });
        }

        border.PointerPressed += async (s, e) =>
        {
            e.Handled = true;
            await OnMentionClicked(userId);
        };

        Inlines?.Add(new InlineUIContainer { Child = border });
    }

    private async Task OnMentionClicked(string userId)
    {
        var user = await AppServices.Chat.GetUser(userId);
        if (user == null) return;

        await TaskService.RunDispatcherAsync(() =>
        {
            var xaml = @"
                <Grid xmlns='https://github.com/avaloniaui'
                      xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                      xmlns:asyncImageLoader='clr-namespace:AsyncImageLoader;assembly=AsyncImageLoader.Avalonia'
                      RowDefinitions='Auto,Auto,Auto'
                      ColumnDefinitions='Auto,8,Auto'
                      Margin='4'>
                    <Border Grid.Row='0' Grid.Column='0' Grid.RowSpan='3'
                            CornerRadius='8' ClipToBounds='True'
                            Width='64' Height='64'>
                        <Image Width='64' Height='64' 
                               asyncImageLoader:ImageLoader.Source='{Binding AvatarUrl}' />
                    </Border>
                    
                    <TextBlock Grid.Row='0' Grid.Column='2'
                               Text='{Binding DisplayName}'
                               Foreground='{Binding Brush}'
                               FontWeight='SemiBold'
                               FontSize='16' />
                    
                    <TextBlock Grid.Row='1' Grid.Column='2'
                               Text='{Binding Username}'
                               FontSize='14'
                               Opacity='0.7' />
                    
                    <TextBlock Grid.Row='2' Grid.Column='2'
                               Text='{Binding Role}'
                               FontSize='12'
                               Opacity='0.5' />
                </Grid>";

            var content = AvaloniaRuntimeXamlLoader.Parse<Grid>(xaml);
            content.DataContext = new
            {
                AvatarUrl = user.AvatarUrl,
                DisplayName = user.DisplayName,
                Brush = user.Brush,
                Username = $"@{user.UserName}",
                Role = user.Role.ToString()
            };
            
            var flyout = new Flyout
            {
                Content = content,
                Placement = PlacementMode.Bottom
            };

            if (this.GetVisualDescendants().OfType<Border>().FirstOrDefault() is { } targetBorder)
            {
                flyout.ShowAt(targetBorder);
            }
            else
            {
                flyout.ShowAt(this);
            }
        });
    }
}