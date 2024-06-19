using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.Export;
using FortnitePorting.Models.Sockets;
using FortnitePorting.Services;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Multiplayer.Data;
using FortnitePorting.Shared.Models.Serilog;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ChatView : ViewBase<ChatViewModel>
{
    public ChatView()
    {
        InitializeComponent();
        ViewModel.Scroll = Scroll;
        TextBox.AddHandler(KeyDownEvent, Handler, RoutingStrategies.Tunnel);

        void Handler(object? sender, KeyEventArgs e)
        {
            if (sender is not AutoCompleteBox autoCompleteBox) return;
            if (autoCompleteBox.GetVisualDescendants().FirstOrDefault(x => x is TextBox) is not TextBox textBox) return;
            if (textBox.Text is not { } text) return;
            if (string.IsNullOrWhiteSpace(text)) return;
            
            if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (text.StartsWith("/export"))
                {
                    var targetName = text.SubstringAfter(" ").SubstringBefore(" ");
                    var path = text.SubstringAfter(" ").SubstringAfter(" ");
                    
                    SocketService.Send(new ExportData(targetName, Exporter.FixPath(path)));
                }
                else if (text.StartsWith("/message"))
                {
                    var targetName = text.SubstringAfter(" ").SubstringBefore(" ");
                    var message = text.SubstringAfter(" ").SubstringAfter(" ");
                    
                    SocketService.Send(new DirectMessageData(targetName, message));
                }
                else if (text.StartsWith("/shrug"))
                {
                    SocketService.Send(new MessageData(@"¯\_(ツ)_/¯"));
                }
                else
                {
                    SocketService.Send(new MessageData(text));
                }
                textBox.Text = string.Empty;
                Scroll.ScrollToEnd();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                textBox.Text += "\n";
                textBox.CaretIndex = textBox.Text.Length;
                e.Handled = true;
            }
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        Scroll.ScrollToEnd();
    }

    private void OnYeahPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not ChatMessage message) return;

        message.ReactedTo = !message.ReactedTo;
        SocketService.Send(new ReactionData(message.Id, SocketService.User.ID, message.ReactedTo));
    }
}