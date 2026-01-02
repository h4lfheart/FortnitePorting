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
using FortnitePorting.Views;
using Mapster;
using Serilog;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.Models;
using Constants = Supabase.Postgrest.Constants;

namespace FortnitePorting.Services;

public partial class ChatService : ObservableObject, IService
{
    [ObservableProperty] private SupabaseService _supaBase;
    
    public ChatService(SupabaseService supaBase)
    {
        SupaBase = supaBase;
    }

    public event EventHandler<ChatMessage>? MessageReceived; 
    
    [ObservableProperty] private ObservableDictionary<string, ChatMessage> _messages = [];
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(UsersByGroup)), NotifyPropertyChangedFor(nameof(UserMentionNames))] 
    private ObservableDictionary<string, ChatUser> _users = [];

    public IEnumerable<string> UserMentionNames => Users.Select(user => $"@{user.Value.UserName}");
    
    [ObservableProperty] private ObservableCollection<ChatUser> _typingUsers = [];

    public string? TypingUsersText => TypingUsers.Count > 0
        ? $"{(TypingUsers.Count > 4 ? "Several users" : TypingUsers.Select(user => user.DisplayName).CommaJoin())} {(TypingUsers.Count > 1 ? "are" : "is")} typing..."
        : null;

    public ObservableDictionary<ESupabaseRole, List<ChatUser>> UsersByGroup => new(Users
            .Select(user => user.Value)
            .OrderBy(user => user.DisplayName)
            .GroupBy(user => user.Role)
            .OrderByDescending(group => group.Key)
            .ToDictionary(group => group.Key, group => group.ToList())
    );
    
    [ObservableProperty] private ObservableDictionary<string, ChatUser> _userCache = [];

    [ObservableProperty] private ChatUserPresence _presence;

    private RealtimeChannel _chatChannel;
    public RealtimePresence<ChatUserPresence> ChatPresence;
    private RealtimeBroadcast<BaseBroadcast> _chatBroadcast;

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
        
        await ChatPresence.Track(Presence);
        
        var messages = await SupaBase.Client.From<Message>()
            .Order("timestamp", Constants.Ordering.Ascending)
            .Limit(50)
            .Get();
        
        foreach (var message in messages.Models)
        {
            if (message.ReplyId is not null)
            {
                Messages[message.ReplyId].ReplyMessages.AddOrUpdate(message.Id, message.Adapt<ChatMessage>());
            }
            else
            {
                Messages.AddOrUpdate(message.Id, message.Adapt<ChatMessage>());
            }
        }

        MessageReceived += (sender, args) =>
        {
            if (Navigation.App.IsTabOpen<ChatView>()) return;
            if (!args.Text.Contains($"@{SupaBase.UserInfo.UserName}")) return;
            
            Info.Message($"Chat Message from {args.User.DisplayName}", args.Text, autoClose: false);
        };
    }

    public async Task Uninitialize()
    {
        await ChatPresence.Untrack();
        _chatChannel.Unsubscribe();
    }

    private async Task InitializePresence()
    {
        if (ChatPresence is not null) return;
        
        ChatPresence = _chatChannel.Register<ChatUserPresence>(SupaBase.UserInfo!.UserId);

        TypingUsers.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(TypingUsersText));
        ChatPresence.AddPresenceEventHandler(IRealtimePresence.EventType.Sync, (sender, type) =>
        {
            TaskService.Run(async () =>
            {
                TypingUsers.Clear();
                
                var currentState = ChatPresence.CurrentState.ToDictionary();
                foreach (var (presenceId, presences) in currentState)
                {
                    var targetPresence = presences.Last();
                    
                    var targetUser = await GetUser(targetPresence.UserId) ?? await GetUser(presenceId);
                    if (targetUser?.UserId is null || targetUser.UserId.Equals(SupaBase.UserInfo.UserId)) continue;
                    
                    if (targetPresence.IsTyping)
                        TypingUsers.Add(targetUser);
                }
            });
        });

        ChatPresence.AddPresenceEventHandler(IRealtimePresence.EventType.Join, (sender, type) =>
        {
            TaskService.Run(async () =>
            {
                var currentState = ChatPresence.CurrentState.ToDictionary();
                var newUsers = 0;
                foreach (var (presenceId, presences) in currentState)
                {
                    var targetPresence = presences.Last();
                    
                    var targetUser = await GetUser(targetPresence.UserId) ?? await GetUser(presenceId);
                    if (targetUser?.UserId is null) continue;
                    
                    targetUser.Tag = targetPresence.Application;
                    targetUser.Version = targetPresence.Version;
                    
                    if (Users.ContainsKey(targetUser.UserId)) continue;
                    
                    Users.AddOrUpdate(targetPresence.UserId, targetUser);
                    newUsers++;
                }
                
                if (newUsers > 0)
                    OnPropertyChanged(nameof(UsersByGroup));
                
                Discord.Update($"Chatting with {Users.Count} {(Users.Count > 1 ? "Users" : "User")}");
            });
        });

        ChatPresence.AddPresenceEventHandler(IRealtimePresence.EventType.Leave, (sender, type) =>
        {
            var currentState = ChatPresence.CurrentState;
            var removedUsers = 0;
            foreach (var user in Users.ToArray())
            {
                if (!currentState.ContainsKey(user.Key))
                {
                    Users.Remove(user.Key);
                    removedUsers++;
                }
            }
            
            if (removedUsers > 0)
                OnPropertyChanged(nameof(UsersByGroup));
        });
        
    }

    private async Task InitializeBroadcasts()
    {
        if (_chatBroadcast is not null) return;
        
        _chatBroadcast = _chatChannel.Register<BaseBroadcast>();
        _chatBroadcast.AddBroadcastEventHandler((sender, broadcast) =>
        {
            if (broadcast is null) return;

            switch (broadcast.Event)
            {
                case "insert_message":
                {
                    var message = broadcast.Get<Message>("message").Adapt<ChatMessage>();
                    if (message.ReplyId is not null)
                    {
                        Messages[message.ReplyId].ReplyMessages.AddOrUpdate(message.Id, message);
                    }
                    else
                    {
                        Messages.AddOrUpdate(message.Id, message);
                    }
                    
                    
                    MessageReceived?.Invoke(this, message);
                    
                    break;
                }
                case "update_message":
                {
                    var updatedMessage = broadcast.Get<Message>("message").Adapt<ChatMessage>();
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

    public async Task<ChatUser?> GetUser(string id)
    {
        if (UserCache.TryGetValue(id, out var existingUser)) return existingUser;

        var userInfo = await Api.FortnitePorting.UserInfo(id);
        if (userInfo is null) return null;
        
        var user = userInfo.Adapt<ChatUser>();
        UserCache[id] = user;
        
        return user;
    }

    public async Task SendMessage(string text, string? replyId = null, string? imagePath = null)
    {
        await Api.FortnitePorting.PostMessage(text, replyId, imagePath);
    }
    
    public async Task UpdateMessage(ChatMessage message, string text)
    {
        await Api.FortnitePorting.EditMessage(text, message.Id);
    }
}