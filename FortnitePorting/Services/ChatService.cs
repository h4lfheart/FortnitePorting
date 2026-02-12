using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using Mapster;
using ReactiveUI;
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
    
    [ObservableProperty] private ReadOnlyObservableCollection<ChatMessage> _messages = new([]);
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(UsersByGroup)), NotifyPropertyChangedFor(nameof(UserMentionNames))] 
    private ObservableDictionary<string, ChatUser> _users = [];

    public IEnumerable<string> UserMentionNames
    {
        get
        {
            var baseUsers = Users.Select(user => $"@{user.Value.UserName}");
            if (SupaBase.UserInfo?.Role >= ESupabaseRole.Staff)
                baseUsers = baseUsers.Concat(["@everyone"]);

            return baseUsers;
        }
    }
    
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

    private SourceCache<ChatMessage, string> _messageCache = new(message => message.Id);
    
    private readonly SemaphoreSlim _userGetLock = new(1, 1);
    private readonly SemaphoreSlim _messageFetchLock = new(1, 1);

    // Pagination state
    private const int PageSize = 20;
    [ObservableProperty] private bool _isLoadingMessages = false;
    [ObservableProperty] private bool _hasMoreMessages = true;
    private DateTime? _oldestFetchedTimestamp = null;

    public async Task Initialize()
    {
        _chatChannel = SupaBase.Client.Realtime.Channel("chat");

        _messageCache.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Sort(SortExpressionComparer<ChatMessage>.Ascending(item => item.Timestamp))
            .Bind(out var messageCollection)
            .Subscribe();

        Messages = messageCollection;

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
        
        await LoadMoreMessages();

        MessageReceived += (sender, message) =>
        {
            if (message.IsPing && !Navigation.App.IsTabOpen<ChatView>())
                Info.Message($"Chat Message from {message.User.DisplayName}", ConvertIdsToMentions(message.Text), autoClose: false);
        };
    }

    public async Task Uninitialize()
    {
        await ChatPresence.Untrack();
        _chatChannel.Unsubscribe();
    }

    public async Task<bool> LoadMoreMessages()
    {
        if (IsLoadingMessages || !HasMoreMessages)
            return false;

        await _messageFetchLock.WaitAsync();
        try
        {
            IsLoadingMessages = true;

            var query = SupaBase.Client.From<Message>()
                .Order("timestamp", Constants.Ordering.Descending)
                .Limit(PageSize + 1);

            if (_oldestFetchedTimestamp.HasValue)
            {
                var utcTimestamp = _oldestFetchedTimestamp.Value.ToUniversalTime();
                query = query.Filter("timestamp", Constants.Operator.LessThanOrEqual, utcTimestamp.ToString("o"));
            }

            var response = await query.Get();
            var messages = response.Models.ToList();
            var originalCount = messages.Count;

            if (_oldestFetchedTimestamp.HasValue && messages.Count > 0)
            {
                messages = messages.Skip(1).ToList();
            }

            if (messages.Count == 0)
            {
                HasMoreMessages = false;
                return false;
            }

            _oldestFetchedTimestamp = messages.Last().Timestamp.ToUniversalTime();
            
            if (originalCount < PageSize + 1)
            {
                HasMoreMessages = false;
            }

            var addedCount = 0;
            foreach (var message in messages.OrderBy(x => x.ReplyId is not null))
            {
                if (_messageCache.Lookup(message.Id).HasValue) continue;
                
                AddMessage(message, isInit: true);
                addedCount++;
            }

            return addedCount > 0;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            IsLoadingMessages = false;
            _messageFetchLock.Release();
        }
    }

    private async Task InitializePresence()
    {
        if (ChatPresence is not null) return;
        
        ChatPresence = _chatChannel.Register<ChatUserPresence>(SupaBase.UserInfo.UserId);

        TypingUsers.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(TypingUsersText));
        ChatPresence.AddPresenceEventHandler(IRealtimePresence.EventType.Sync, (sender, type) =>
        {
            if (!SupaBase.IsLoggedIn) return;
            
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
            if (!SupaBase.IsLoggedIn) return;
            
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
                
                if (Navigation.App.IsTabOpen<ChatView>())
                    Discord.Update($"Chatting with {Users.Count} {(Users.Count > 1 ? "Users" : "User")}");
            });
        });

        ChatPresence.AddPresenceEventHandler(IRealtimePresence.EventType.Leave, (sender, type) =>
        {
            if (!SupaBase.IsLoggedIn) return;
            
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
            if (!SupaBase.IsLoggedIn) return;
            if (broadcast is null) return;

            switch (broadcast.Event)
            {
                case "insert_message":
                {
                    var message = broadcast.Get<Message>("message");
                    AddMessage(message);
                    
                    break;
                }
                case "update_message":
                {
                    var updatedMessage = broadcast.Get<Message>("message").Adapt<ChatMessage>();
                    var targetMessage = updatedMessage.ReplyId is not null
                        ? _messageCache.Lookup(updatedMessage.ReplyId).Value.ReplyMessages.FirstOrDefault(reply => reply.Id.Equals(updatedMessage.Id))
                        : _messageCache.Lookup(updatedMessage.Id).Value;

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
                        _messageCache.Lookup(replyId).Value.ReplyMessages.RemoveAll(reply => reply.Id.Equals(replyId));
                    }
                    else
                    {
                        _messageCache.Remove(messageId);
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

    public void AddMessage(Message inMessage, bool isInit = false)
    {
        var message = inMessage.Adapt<ChatMessage>();
        if (message.ReplyId is not null)
        {
            if (_messageCache.Lookup(message.ReplyId) is { HasValue: true } replyParent)
                replyParent.Value.ReplyMessages.Add(message);
        }
        else
        {
            _messageCache.AddOrUpdate(message);
        }

        if (message.Text.Contains($"<@{SupaBase.UserInfo?.UserId}>") || message.Text.Contains("<@everyone>"))
            message.IsPing = true;
                    
        if (!isInit)
            MessageReceived?.Invoke(this, message);
    }
    
    public string ConvertMentionsToIds(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var mentionPattern = @"@([\w.]+)";
        var regex = new Regex(mentionPattern);
    
        return regex.Replace(text, match =>
        {
            var username = match.Groups[1].Value;
        
            if (username.Equals("everyone", StringComparison.OrdinalIgnoreCase))
                return "<@everyone>";
        
            var user = Chat.Users.FirstOrDefault(x => 
                x.Value.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));
        
            return user != null ? $"<@{user.Value.UserId}>" : match.Value;
        });
    }
    
    public string ConvertIdsToMentions(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var mentionPattern = @"<@([a-f0-9\-]+|everyone)>";
        var regex = new Regex(mentionPattern, RegexOptions.IgnoreCase);

        return regex.Replace(text, match =>
        {
            var userId = match.Groups[1].Value;
    
            if (userId.Equals("everyone", StringComparison.OrdinalIgnoreCase))
                return "@everyone";
    
            var user = Chat.UserCache.FirstOrDefault(x => 
                x.Key.Equals(userId, StringComparison.OrdinalIgnoreCase));
    
            return user?.Value != null ? $"@{user.Value.DisplayName}" : match.Value;
        });
    }
    

    public async Task<ChatUser?> GetUser(string id)
    {
        await _userGetLock.WaitAsync();
        try
        {
            if (UserCache.TryGetValue(id, out var existingUser)) return existingUser;

            var userInfo = await Api.FortnitePorting.UserInfo(id);
            if (userInfo is null) return null;
        
            var user = userInfo.Adapt<ChatUser>();
            UserCache[id] = user;
        
            return user;
        }
        finally
        {
            _userGetLock.Release();
        }
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