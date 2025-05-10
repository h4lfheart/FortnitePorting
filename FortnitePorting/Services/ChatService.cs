using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using Mapster;
using Supabase.Postgrest;
using Supabase.Realtime.PostgresChanges;

namespace FortnitePorting.Services;

public partial class ChatService : ObservableObject, IService
{
    [ObservableProperty] private SupabaseService _supaBase;

    public ChatService(SupabaseService supaBase)
    {
        SupaBase = supaBase;
        
        TaskService.Run(async () =>
        {
            var messages = await SupaBase.Client.From<Message>()
                .Limit(100)
                .Order("timestamp", Constants.Ordering.Ascending)
                .Get();
            
            foreach (var message in messages.Models)
            {
                if (message.ReplyId is not null)
                {
                    Messages[message.ReplyId].ReplyMessages.AddOrUpdate(message.Id, message.Adapt<ChatMessageV2>());
                }
                else
                {
                    Messages.AddOrUpdate(message.Id, message.Adapt<ChatMessageV2>());
                }
            }
        });

        SupaBase.Client.From<Message>().On(PostgresChangesOptions.ListenType.Inserts, (sender, change) =>
        {
            var message = change.Model<Message>();
            if (message is null) return;

            TaskService.Run(async () =>
            {
                if (message.ReplyId is not null)
                {
                    Messages[message.ReplyId].ReplyMessages.AddOrUpdate(message.Id, message.Adapt<ChatMessageV2>());
                }
                else
                {
                    Messages.AddOrUpdate(message.Id, message.Adapt<ChatMessageV2>());
                }
            });
        });
        
        SupaBase.Client.From<Message>().On(PostgresChangesOptions.ListenType.Updates, (sender, change) =>
        {
            var updatedMessage = change.Model<Message>();
            if (updatedMessage is null) return;

            var targetMessage = Messages.FirstOrDefault(msg => msg.Key.Equals(updatedMessage.Id))?.Value;
            if (targetMessage is null) return;
            
            targetMessage.Text = updatedMessage.Text;
            targetMessage.WasEdited = updatedMessage.WasEdited;
        });
        
        SupaBase.Client.From<Message>().On(PostgresChangesOptions.ListenType.Deletes, (sender, change) =>
        {
            var deletedMessage = change.OldModel<Message>();
            if (deletedMessage is null) return;

            Messages.Remove(deletedMessage.Id);
        });
    }

    [ObservableProperty] private ObservableDictionary<string, ChatMessageV2> _messages = [];

    public async Task SendMessage(string text, string? replyId = null)
    {
        await SupaBase.Client.From<Message>().Insert(new Message
        {
            Text = text,
            Application = Globals.ApplicationTag,
            ReplyId = replyId
        });
    }
    
    public async Task UpdateMessage(ChatMessageV2 message, string text)
    {
        var editedMessage = message.Adapt<Message>();
        editedMessage.Text = text;
        await SupaBase.Client.From<Message>().Update(editedMessage);
    }
}