using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using Mapster;
using Serilog;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.Models;
using Supabase.Realtime.PostgresChanges;
using Constants = Supabase.Postgrest.Constants;

namespace FortnitePorting.Services;

public partial class ChatService : ObservableObject, IService
{
    [ObservableProperty] private SupabaseService _supaBase;
    
    public ChatService(SupabaseService supaBase)
    {
        SupaBase = supaBase;
    }
    
    [ObservableProperty] private ObservableDictionary<string, ChatMessageV2> _messages = [];
    [ObservableProperty] private ObservableDictionary<string, ChatUserV2> _users = [];
    
    [ObservableProperty] private ChatUserPresence _presence = new()
    {
        Application = Globals.ApplicationTag,
        Version = Globals.VersionString
    };

    private RealtimeChannel _chatChannel;
    private RealtimePresence<ChatUserPresence> _chatPresence;

    public async Task Initialize()
    {
        _chatChannel = SupaBase.Client.Realtime.Channel("chat");

        await InitializePresence();
        await InitializeBroadcasts();
        
        await _chatChannel.Subscribe();
        await _chatPresence.Track(Presence);
        
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
    }

    private async Task InitializePresence()
    {
        _chatPresence = _chatChannel.Register<ChatUserPresence>(SupaBase.UserInfo!.UserId);
        
        _chatPresence.AddPresenceEventHandler(IRealtimePresence.EventType.Sync, (sender, type) =>
        {
            
        });

        _chatPresence.AddPresenceEventHandler(IRealtimePresence.EventType.Join, async (sender, type) =>
        {
            var currentState = _chatPresence.CurrentState;
            foreach (var (userId, presences) in currentState)
            {
                if (Users.ContainsKey(userId)) continue;

                var userInfo = await Api.FortnitePorting.UserInfo(userId);
                if (userInfo is null) continue;

                var newestPresence = presences.Last();
                
                var user = userInfo.Adapt<ChatUserV2>();
                user.Tag = newestPresence.Application;
                user.Version = newestPresence.Version;
                Users.AddOrUpdate(userId, user);
            }
        });

        _chatPresence.AddPresenceEventHandler(IRealtimePresence.EventType.Leave, (sender, type) =>
        {
            var currentState = _chatPresence.CurrentState;
            foreach (var user in Users)
            {
                if (!currentState.ContainsKey(user.Key))
                {
                    Users.Remove(user.Key);
                }
            }
        });
        
    }

    private async Task InitializeBroadcasts()
    {
        var broadcastChannel = _chatChannel.Register<BaseBroadcast>();
        broadcastChannel.AddBroadcastEventHandler((sender, broadcast) =>
        {
            if (broadcast is null) return;

            switch (broadcast.Event)
            {
                case "insert_message":
                case "update_message":
                {
                    var message = broadcast.Get<Message>("message").Adapt<ChatMessageV2>();
                    if (message.ReplyId is not null)
                    {
                        Messages[message.ReplyId].ReplyMessages.AddOrUpdate(message.Id, message);
                    }
                    else
                    {
                        Messages.AddOrUpdate(message.Id, message);
                    }
                    break;
                }
                case "delete_message":
                {
                    var messageId = broadcast.Get<string>("message_id");
                    var replyId = broadcast.Get<string?>("reply_id");
                    
                    if (replyId is not null)
                    {
                        Messages[replyId].ReplyMessages.Remove(messageId);
                    }
                    else
                    {
                        Messages.Remove(messageId);
                    }
                    break;
                }
            }
        });
    }

    public async Task SendMessage(string text, string? replyId = null, string? imagePath = null)
    {
        await SupaBase.Client.From<Message>().Insert(new Message
        {
            Text = text,
            Application = Globals.ApplicationTag,
            ReplyId = replyId,
            ImagePath = imagePath,
        });
    }
    
    public async Task UpdateMessage(ChatMessageV2 message, string text)
    {
        var editedMessage = message.Adapt<Message>();
        editedMessage.Text = text;
        await SupaBase.Client.From<Message>().Update(editedMessage);
    }
}