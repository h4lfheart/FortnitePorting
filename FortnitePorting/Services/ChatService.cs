using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Shared.Extensions;
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

    public event EventHandler? MessageReceived; 
    
    [ObservableProperty] private ObservableDictionary<string, ChatMessageV2> _messages = [];
    [ObservableProperty, NotifyPropertyChangedFor(nameof(UsersByGroup))] private ObservableDictionary<string, ChatUserV2> _users = [];

    public ObservableDictionary<ESupabaseRole, List<ChatUserV2>> UsersByGroup => new(Users
            .Select(user => user.Value)
            .OrderBy(user => user.DisplayName)
            .GroupBy(user => user.Role)
            .OrderByDescending(group => group.Key)
            .ToDictionary(group => group.Key, group => group.ToList())
    );
    
    [ObservableProperty] private ObservableDictionary<string, ChatUserV2> _userCache = [];

    [ObservableProperty] private ChatUserPresence _presence;

    private RealtimeChannel _chatChannel;
    private RealtimePresence<ChatUserPresence> _chatPresence;
    private RealtimeBroadcast<BaseBroadcast> _chatBrodcast;

    public async Task Initialize()
    {
        _chatChannel = SupaBase.Client.Realtime.Channel("chat");

        await InitializePresence();
        await InitializeBroadcasts();
        
        await _chatChannel.Subscribe();
        
        Presence = new ChatUserPresence
        {
            UserId = SupaBase.UserInfo!.UserId,
            Application = Globals.ApplicationTag,
            Version = Globals.VersionString
        };
        
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

    public async Task Uninitialize()
    {
        await _chatPresence.Untrack();
        _chatChannel.Unsubscribe();
    }

    private async Task InitializePresence()
    {
        if (_chatPresence is not null) return;
        
        _chatPresence = _chatChannel.Register<ChatUserPresence>(SupaBase.UserInfo!.UserId);
        
        _chatPresence.AddPresenceEventHandler(IRealtimePresence.EventType.Sync, (sender, type) =>
        {
            
        });

        _chatPresence.AddPresenceEventHandler(IRealtimePresence.EventType.Join, (sender, type) =>
        {
            TaskService.Run(async () =>
            {
                var currentState = _chatPresence.CurrentState.ToDictionary();
                foreach (var (presenceId, presences) in currentState)
                {
                    var targetPresence = presences.Last();
                    
                    var targetUser = await GetUser(targetPresence.UserId) ?? await GetUser(presenceId);
                    if (targetUser is null) continue;
                    if (targetUser.UserId is null) continue;
                    
                    targetUser.Tag = targetPresence.Application;
                    targetUser.Version = targetPresence.Version;
                    
                    if (Users.ContainsKey(targetUser.UserId)) continue;
                    
                    Users.AddOrUpdate(targetPresence.UserId, targetUser);
                }
                
                OnPropertyChanged(nameof(UsersByGroup));
            });
        });

        _chatPresence.AddPresenceEventHandler(IRealtimePresence.EventType.Leave, (sender, type) =>
        {
            var currentState = _chatPresence.CurrentState;
            foreach (var user in Users.ToArray())
            {
                if (!currentState.ContainsKey(user.Key))
                {
                    Users.Remove(user.Key);
                }
            }
            
            OnPropertyChanged(nameof(UsersByGroup));
        });
        
    }

    private async Task InitializeBroadcasts()
    {
        if (_chatBrodcast is not null) return;
        
        _chatBrodcast = _chatChannel.Register<BaseBroadcast>();
        _chatBrodcast.AddBroadcastEventHandler((sender, broadcast) =>
        {
            if (broadcast is null) return;

            switch (broadcast.Event)
            {
                case "insert_message":
                {
                    broadcast.Get<Message>("message");
                    
                    var message = broadcast.Get<Message>("message").Adapt<ChatMessageV2>();
                    if (message.ReplyId is not null)
                    {
                        Messages[message.ReplyId].ReplyMessages.AddOrUpdate(message.Id, message);
                    }
                    else
                    {
                        Messages.AddOrUpdate(message.Id, message);
                    }
                    
                    MessageReceived?.Invoke(this, EventArgs.Empty);
                    
                    break;
                }
                case "update_message":
                {
                    var updatedMessage = broadcast.Get<Message>("message").Adapt<ChatMessageV2>();
                    var targetMessage = updatedMessage.ReplyId is not null
                        ? Messages[updatedMessage.ReplyId].ReplyMessages[updatedMessage.Id]
                        : Messages[updatedMessage.Id];

                    targetMessage.Text = updatedMessage.Text;
                    targetMessage.ReactorIds = updatedMessage.ReactorIds;
                    targetMessage.WasEdited = updatedMessage.WasEdited;
                    
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
                case "update_permissions":
                {
                    var userId = broadcast.Get<string>("user_id");
                    var role = broadcast.Get<ESupabaseRole>("role");

                    UserCache.UpdateIfContains(userId, user => user.Role = role);
                    OnPropertyChanged(nameof(UsersByGroup));
                    break;
                }
            }
        });
    }

    public async Task<ChatUserV2?> GetUser(string id)
    {
        if (UserCache.TryGetValue(id, out var existingUser)) return existingUser;

        var userInfo = await Api.FortnitePorting.UserInfo(id);
        if (userInfo is null) return null;
        
        var user = userInfo.Adapt<ChatUserV2>();
        UserCache[id] = user;
        
        return user;
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