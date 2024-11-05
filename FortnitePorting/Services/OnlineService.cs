using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using DesktopNotifications;
using DynamicData;
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Export;
using FortnitePorting.Models.Canvas;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.OnlineServices.Extensions;
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.ViewModels.Settings;
using FortnitePorting.Views;
using Microsoft.Toolkit.Uwp.Notifications;
using Org.BouncyCastle.Asn1.X509;
using Serilog;
using Serilog.Core;
using WatsonTcp;
using Exception = System.Exception;
using MultiplayerGlobals = FortnitePorting.OnlineServices.MultiplayerGlobals;

namespace FortnitePorting.Services;

public static class OnlineService
{
    public static EPermissions Permissions;
    
    public static bool EstablishedFirstConnection;
    public static WatsonTcpClient? Client;

    public static MetadataBuilder DefaultMeta => new MetadataBuilder()
        .With("Token", AppSettings.Current.Online.Auth.Token);
    
    public static DisconnectReason DisconnectReason;
    
    public static void Init()
    {
        try
        {
            Client = new WatsonTcpClient(MultiplayerGlobals.SOCKET_IP, MultiplayerGlobals.SOCKET_PORT);
            Client.Settings.Guid = AppSettings.Current.Online.Identification.Identifier;
            Client.Callbacks.SyncRequestReceivedAsync = SyncRequestReceivedAsync;
            Client.Events.MessageReceived += OnMessageReceived;
            Client.Events.ServerDisconnected += (sender, args) =>
            {
                if (args.Reason == DisconnectReason.AuthFailure)
                {
                    AppWM.Dialog("Disconnected",
                        "You have been disconnected from the online services due to an invalid authentication. Please re-authenticate in online settings");
                }
                
                DisconnectReason = args.Reason;
            };
            
            TaskService.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (Client is null) break;

                        if (Client.Connected && ChatVM is not null)
                        {
                            ChatVM.AreServicesDown = false;
                        }

                        while (!Client.Connected && AppSettings.Current.Online.UseIntegration &&
                               DisconnectReason != DisconnectReason.AuthFailure)
                        {
                            try
                            {
                                Client.Connect();
                            }
                            catch (Exception e)
                            {
                                Log.Error(e.ToString());
                                ChatVM.AreServicesDown = true;
                            }

                            await Task.Delay(1000);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                    }
                }
            });
        }
        catch (Exception e)
        {
            // todo visual handling popup so user isnt confused
            Log.Error(e.ToString());
        }
    }

    public static void DeInit()
    {
        if (Client is null) return;
        if (!Client.Connected) return;
        
        Client.Disconnect();
        Client.Dispose();
        Client = null;
    }

    private static async Task<SyncResponse> SyncRequestReceivedAsync(SyncRequest arg)
    {
        var type = arg.GetArgument<EPacketType>("Type");
        switch (type)
        {
            case EPacketType.Connect:
            {
                var meta = DefaultMeta
                    .With("RequestingMessageHistory", !EstablishedFirstConnection)
                    .With("MessageFetchCount", AppSettings.Current.Online.MessageFetchCount)
                    .With("Version", Globals.VersionString)
                    .Build();
                var response = new SyncResponse(arg, meta, Array.Empty<byte>());
                EstablishedFirstConnection = true;
                return response;
            }
            case EPacketType.Ping:
            {
                return new SyncResponse(arg, DefaultMeta.Build(), Array.Empty<byte>());
            }
            default:
            {
                throw new NotImplementedException(type.ToString());
            }
        }
    }

    private static async void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        var type = e.GetArgument<EPacketType>("Type");
        var user = e.GetArgument<Identification>("User")!;
        switch (type)
        {
            case EPacketType.Permissions:
            {
                var permissions = e.Data.ReadPacket<PermissionsPacket>();

                var permissionEnum = permissions.Permissions; // lol
                
                if (HelpVM is not null) HelpVM.Permissions = permissionEnum;
                ChatVM.Permissions = permissionEnum;
                Permissions = permissionEnum;
                
                await TaskService.RunDispatcherAsync(() => // random invalid thread exception, this should fix
                {
                    ChatVM.Commands.Clear();
                    ChatVM.Commands.AddRange(permissions.Commands);
                });
                break;
            }
            case EPacketType.Message:
            {
                var messagePacket = e.Data.ReadPacket<MessagePacket>();
                
                Bitmap? bitmap = null;
                if (messagePacket.HasAttachmentData)
                {
                    try
                    {
                        var stream = new MemoryStream(messagePacket.AttachmentData);
                        bitmap = new Bitmap(stream);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        break;
                    }
                }

                var isInChatView = AppWM.IsInView<ChatView>();
                var isPrivate = e.GetArgument<bool>("IsPrivate");
                var isPing = messagePacket.Message.Contains("@everyone") && user.HasPermission(EPermissions.Staff) 
                             || messagePacket.Message.Contains($"@{AppSettings.Current.Online.GlobalName}");
                if (AppSettings.Current.Online.OnlineStatus == EOnlineStatus.Online && !isInChatView && (isPrivate || isPing))
                {
                    var notification = new Notification
                    {
                        Title = $"Message from {user.DisplayName}",
                        Body = messagePacket.Message
                    };
                    
                    await NotificationManager.ShowNotification(notification);
                }

                if (!e.GetArgument<bool>("IsMessageHistory")
                    && user.RoleType is not ERoleType.System
                    && AppSettings.Current.Online.OnlineStatus is EOnlineStatus.Online
                    && !isInChatView)
                {
                    AppWM.ChatNotifications++;
                }
                
                ChatVM.Messages.InsertSorted(new ChatMessage
                {
                    User = new ChatUser
                    {
                        DisplayName = user.DisplayName,
                        UserName = user.UserName,
                        ProfilePictureURL = user.AvatarURL,
                        Id = user.Id,
                        Role = user.RoleType,
                        Version = user.Version
                    },
                    Text = messagePacket.Message,
                    Id = e.GetArgument<Guid>("ID"),
                    Timestamp = e.GetArgument<DateTime>("Timestamp").ToLocalTime(),
                    Bitmap = bitmap,
                    BitmapName = messagePacket.AttachmentName,
                    ReactionCount = e.GetArgument<int>("Reactions"),
                    ReactedTo = e.GetArgument<bool>("HasReacted"),
                    IsPrivate = isPrivate,
                    TargetUserName = e.GetArgument<string>("TargetUserName"),
                    IsPing = isPing
                }, SortExpressionComparer<ChatMessage>.Descending(message => message.Timestamp));
                break;
            }
            case EPacketType.OnlineUsers:
            {
                var onlineUsers = e.Data.ReadPacket<OnlineUsersPacket>().Users;
                
                for (var i = 0; i < ChatVM.Users.Count; i++)
                {
                    if (onlineUsers.Any(onlineUser => ChatVM.Users[i].UserName.Equals(onlineUser.UserName))) continue;
                    ChatVM.Users.RemoveAt(i);
                }
                
                for (var i = 0; i < onlineUsers.Count; i++)
                {
                    var onlineUser = onlineUsers[i];
                    if (ChatVM.Users.FirstOrDefault(existingUser => existingUser.UserName.Equals(onlineUser.UserName)) 
                        is { } realUser)
                    {
                        realUser.Role = onlineUser.RoleType; // update role if changed
                        continue;
                    }
                    
                    ChatVM.Users.InsertSorted(new ChatUser
                    {
                        DisplayName = onlineUser.DisplayName,
                        UserName = onlineUser.UserName,
                        ProfilePictureURL = onlineUser.AvatarURL,
                        Id = onlineUser.Id,
                        Role = onlineUser.RoleType,
                        Version = onlineUser.Version
                    }, SortExpressionComparer<ChatUser>.Ascending(sortedUser => sortedUser.Role));
                }
                
                break;
            }
            case EPacketType.Reaction:
            {
                var reaction = e.Data.ReadPacket<ReactionPacket>();
                var targetMessage = ChatVM.Messages.FirstOrDefault(message => message.Id == reaction.MessageId);
                if (targetMessage is null) break;

                targetMessage.ReactionCount += reaction.Increment ? 1 : -1;
                break;
            }
            case EPacketType.Export:
            {
                if (AppSettings.Current.Online.OnlineStatus == EOnlineStatus.DoNotDisturb) break;
                
                var export = e.Data.ReadPacket<ExportPacket>();
                
                await TaskService.RunDispatcherAsync(async () =>
                {
                    var xaml =
                        """
                            <ContentControl xmlns="https://github.com/avaloniaui"
                                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                        xmlns:ext="clr-namespace:FortnitePorting.Shared.Extensions;assembly=FortnitePorting.Shared"
                                        xmlns:shared="clr-namespace:FortnitePorting.Shared;assembly=FortnitePorting.Shared">
                                <StackPanel HorizontalAlignment="Stretch">
                                    <TextBlock Text="{Binding DisplayText}" TextWrapping="Wrap"/>
                                    <ComboBox x:Name="ExportLocationBox" SelectedIndex="0" Margin="{ext:Space 0, 1, 0, 0}"
                                              ItemsSource="{ext:EnumToItemsSource {x:Type shared:EExportLocation}}"
                                              HorizontalAlignment="Stretch">
                                        <ComboBox.ItemContainerTheme>
                                            <ControlTheme x:DataType="ext:EnumRecord" TargetType="ComboBoxItem" BasedOn="{StaticResource {x:Type ComboBoxItem}}">
                                                <Setter Property="IsEnabled" Value="{Binding !IsDisabled}"/>
                                            </ControlTheme>
                                        </ComboBox.ItemContainerTheme>
                                    </ComboBox>
                                </StackPanel>
                            </ContentControl>
                        """;
                    
                    var content = xaml.CreateXaml<ContentControl>(new
                    {
                        DisplayText = !string.IsNullOrWhiteSpace(export.Message) 
                            ? $"{user.DisplayName} says \"{export.Message}\" with a request to export \"{export.Path}.\" Accept?"
                            : $"{user.DisplayName} sent a request to export \"{export.Path}.\" Accept?"
                    });
                    
                    var comboBox = content.FindControl<ComboBox>("ExportLocationBox");
                    
                    var dialog = new ContentDialog
                    {
                        Title = "Incoming Export Request",
                        Content = content,
                        CloseButtonText = "No",
                        PrimaryButtonText = "Yes",
                        PrimaryButtonCommand = new RelayCommand(async () =>
                        {
                            var asset = await CUE4ParseVM.Provider.TryLoadObjectAsync(Exporter.FixPath(export.Path));
                            if (asset is null) return;

                            var exportType = Exporter.DetermineExportType(asset);
                            if (exportType is EExportType.None)
                            {
                                AppWM.Dialog("Unimplemented Exporter",
                                    $"A file exporter for \"{asset.ExportType}\" assets has not been implemented and/or will not be supported.");
                                return;
                            }

                            var enumRecord = (EnumRecord) comboBox.SelectedItem;
                            var exportLocation = (EExportLocation) enumRecord.Value;
                            await Exporter.Export(asset, exportType, AppSettings.Current.CreateExportMeta(exportLocation));
                                
                            if (AppSettings.Current.Online.UseIntegration)
                            {
                                await ApiVM.FortnitePorting.PostExportAsync(new PersonalExport(asset.GetPathName()));
                            }

                        })
                    };

                    await dialog.ShowAsync();
                });

                break;
            }
            case EPacketType.MessageHistory:
            {
                var messageHistory = e.Data.ReadPacket<MessageHistoryPacket>();

                var messages = new List<ChatMessage>();
                for (var index = 0; index < messageHistory.Packets.Count; index++)
                {
                    var messagePacket = messageHistory.Packets[index];
                    
                    Bitmap? bitmap = null;
                    if (messagePacket.HasAttachmentData)
                    {
                        try
                        {
                            var stream = new MemoryStream(messagePacket.AttachmentData);
                            bitmap = new Bitmap(stream);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                            continue;
                        }
                    }
                    
                    var messageUser = GetArgument<Identification>("User")!;
                    var isPing = messagePacket.Message.Contains("@everyone") && messageUser.HasPermission(EPermissions.Staff) 
                                 || messagePacket.Message.Contains($"@{AppSettings.Current.Online.GlobalName}");
                    
                    messages.InsertSorted(new ChatMessage
                    {
                        User = new ChatUser
                        {
                            DisplayName = messageUser.DisplayName,
                            UserName = messageUser.UserName,
                            ProfilePictureURL = messageUser.AvatarURL,
                            Id = messageUser.Id,
                            Role = messageUser.RoleType,
                            Version = messageUser.Version
                        },
                        Text = messagePacket.Message,
                        Id = GetArgument<Guid>("ID"),
                        Timestamp = GetArgument<DateTime>("Timestamp").ToLocalTime(),
                        Bitmap = bitmap,
                        BitmapName = messagePacket.AttachmentName,
                        ReactionCount = GetArgument<int>("Reactions"),
                        ReactedTo = GetArgument<bool>("HasReacted"),
                        IsPing = isPing
                    }, SortExpressionComparer<ChatMessage>.Descending(message => message.Timestamp));
                    continue;

                    T? GetArgument<T>(string name)
                    {
                        var meta = messageHistory.Metas[index].Build();
                        return (T?) meta!.GetValueOrDefault(name, null);
                    }
                }

                ChatVM.Messages = [..messages];
                
                break;
            }

            case EPacketType.CanvasPixel:
            {
                if (!AppWM.IsInView<CanvasView>()) break;
                if (CanvasVM.BitmapSource is null) break;
                
                var packet = e.Data.ReadPacket<CanvasPixelPacket>();
                var pixel = packet.Pixel;
                
                CanvasVM.PixelMetadata[new Point(pixel.X, pixel.Y)] = new PixelMetadata(pixel.Name);

                var color = new Color(byte.MaxValue, pixel.R, pixel.G, pixel.B);
                
                using (var framebuffer = CanvasVM.BitmapSource.Lock())
                {
                    unsafe
                    {
                        var buffer = framebuffer.Address;
                        var ptr = (int*) buffer.ToPointer();
            
                        var offset = pixel.Y * CanvasVM.Width + pixel.X;
                        ptr[offset] = (color.A << 24) | (color.B << 16) | (color.G << 8) | color.R;
                    }
                }
                
                await TaskService.RunDispatcherAsync(CanvasVM.BitmapImage.InvalidateVisual);
                
                break;
            }
            case EPacketType.CanvasInfo:
            {
                var packet = e.Data.ReadPacket<CanvasInfoPacket>();

                CanvasVM.Width = packet.X;
                CanvasVM.Height = packet.Y;
                
                var bitmap = new WriteableBitmap(new PixelSize(CanvasVM.Width, CanvasVM.Height), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Opaque);
                using (var framebuffer = bitmap.Lock())
                {
                    unsafe
                    {
                        var width = packet.X;
                        var height = packet.Y;
                        var buffer = framebuffer.Address;

                        var pixelPtr = (int*) buffer;
                        for (var i = 0; i < width * height; i++)
                        {
                            pixelPtr[i] = int.MaxValue;
                        }
                        
                        foreach (var pixel in packet.Pixels)
                        {
                            CanvasVM.PixelMetadata[new Point(pixel.X, pixel.Y)] = new PixelMetadata(pixel.Name);

                            var offset = pixel.Y * width + pixel.X;
                            if (offset > width * height) continue;
                            
                            pixelPtr[offset] = (byte.MaxValue << 24) | (pixel.B << 16) | (pixel.G << 8) | pixel.R;
                        }
                    }
                }

                CanvasVM.BitmapSource = bitmap;
                
                break;
            }
            case EPacketType.CanvasPlacementInfo:
            {
                var packet = e.Data.ReadPacket<CanvasPlacementInfoPacket>();

                CanvasVM.NextPlacementTime = packet.NextPixelTime;
                
                break;
            }
            case EPacketType.Title:
            {
                var packet = e.Data.ReadPacket<TitlePacket>();
                AppWM.Title(packet.Title, packet.Subtitle);
                break;
            }
            default:
            {
                Log.Error($"Receiving invalid packet type: {type.ToString()}");
                break;
            }
        }
    }
    
    public static async Task Send(IPacket packet, MetadataBuilder? additionalMeta = null)
    {
        await Send(packet.WritePacket(), packet.PacketType, additionalMeta);
    }
    
    public static async Task Send(byte[] data, EPacketType type, MetadataBuilder? additionalMeta = null)
    {
        var meta = DefaultMeta
            .WithType(type)
            .With(additionalMeta);

        await Client.SendAsync(data, meta.Build());
    }
}