using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Export;
using FortnitePorting.Models.Assets;
using FortnitePorting.Multiplayer.Data;
using FortnitePorting.Multiplayer.UDP;
using FortnitePorting.Multiplayer.Utils;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using GenericReader;
using Ionic.Zlib;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using ZstdSharp;

namespace FortnitePorting.Services;

public static class SocketService
{
    public static UDPClient Client;

    public static UserData User => new()
    {
        ID = AppSettings.Current.UUID,
        Name = DiscordService.GetDisplayName(),
        AvatarURL = DiscordService.GetAvatarURL()
    };
    
    public static void Init()
    {
        Client = new UDPClient();
        Client.DataReceived += result =>
        {
            var Ar = new GenericBufferReader(result.Buffer);
            var header = Ar.ReadFP<DataHeader>();

            var sender = Ar.ReadFP<UserData>();

            switch (header.DataType)
            {
                case EDataType.Ping:
                {
                    Send(new PingData());
                    break;
                }
                case EDataType.Message:
                {
                    var messageData = Ar.ReadFP<MessageData>();
                    
                    ChatVM.Messages.InsertSorted(new ChatMessage
                    {
                        User = new ChatUser
                        {
                            Name = sender.Name,
                            Avatar = sender.AvatarURL,
                            Id = sender.ID
                        },
                        Text = messageData.Text,
                        Id = messageData.ID,
                        Timestamp = messageData.Time.ToLocalTime(),
                        ReactionCount = messageData.Reactions.Count,
                        ReactedTo = messageData.Reactions.Contains(User.ID)
                    }, SortExpressionComparer<ChatMessage>.Descending(message => message.Timestamp));
                    break;
                }
                case EDataType.Reaction:
                {
                    var reactionData = Ar.ReadFP<ReactionData>();

                    var targetMessage = ChatVM.Messages.FirstOrDefault(message => message.Id == reactionData.MessageID);
                    if (targetMessage is null) break;
                    
                    targetMessage.ReactionCount += reactionData.Increment ? 1 : -1;
                    break;
                }
                case EDataType.OnlineUsers:
                {
                    var onlineUserData = Ar.ReadFP<OnlineUserData>();

                    foreach (var onlineUser in onlineUserData.OnlineUsers)
                    {
                        if (!ChatVM.Users.Any(user => user.Id == onlineUser.ID))
                        {
                            ChatVM.Users.Add(new ChatUser
                            {
                                Name = onlineUser.Name,
                                Avatar = onlineUser.AvatarURL,
                                Id = onlineUser.ID
                            });
                        }
                    }

                    foreach (var existingUser in ChatVM.Users.ToArray())
                    {
                        if (!onlineUserData.OnlineUsers.Any(user => user.ID == existingUser.Id))
                        {
                            ChatVM.Users.Remove(existingUser);
                        }
                    }
                    break;
                }
                case EDataType.Export:
                {
                    var exportData = Ar.ReadFP<ExportData>();
                    
                    TaskService.RunDispatcher(async () =>
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Incoming Export Request",
                            Content = $"{sender.Name} sent a request to export \"{exportData.Path}\". Accept?",
                            CloseButtonText = "No",
                            PrimaryButtonText = "Yes",
                            PrimaryButtonCommand = new RelayCommand(async () =>
                            {
                                var asset = await CUE4ParseVM.Provider.TryLoadObjectAsync(Exporter.FixPath(exportData.Path));
                                if (asset is null) return;
                                
                                var exportType = Exporter.DetermineExportType(asset);
                                if (exportType is EExportType.None)
                                {
                                    DisplayDialog("Unimplemented Exporter", 
                                        $"A file exporter for \"{asset.ExportType}\" assets has not been implemented and/or will not be supported.");
                                }
                                else
                                {
                                    await Exporter.Export(asset, exportType, AppSettings.Current.CreateExportMeta());
                                }

                            })
                        };

                        await dialog.ShowAsync();
                    });
                    
                    break;
                }
                case EDataType.DirectMessage:
                {
                    var directMessageData = Ar.ReadFP<DirectMessageData>();
                    
                    AppVM.Message($"From {sender.Name}", directMessageData.Text);
                    break;
                }
            }
        };

        TaskService.Run(async () =>
        {
            while (true)
            {
                try
                {
                    var received = await Client.Receive();
                    Client.OnDataReceived(received);
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }
        });
    }

    public static void Send(BaseData data)
    {
        Client.Send(data, User);
    }
}