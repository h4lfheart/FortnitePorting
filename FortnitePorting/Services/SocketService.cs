using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Export;
using FortnitePorting.Models.Chat;
using FortnitePorting.Multiplayer;
using FortnitePorting.Multiplayer.Data;
using FortnitePorting.Multiplayer.Utils;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using GenericReader;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WatsonTcp;
using ZstdSharp;

namespace FortnitePorting.Services;

public static class SocketService
{
    public static WatsonTcpClient Client;

    public static UserData User => new()
    {
        ID = AppSettings.Current.Discord.Id,
        Name = AppSettings.Current.Discord.GlobalName,
        AvatarURL = AppSettings.Current.Discord.ProfilePictureURL
    };

    private static Dictionary<Guid, List<BaseData>> IncomingImageData = [];
    
    public static void Init()
    {
        Client = new WatsonTcpClient(MultiplayerGlobals.SOCKET_IP, MultiplayerGlobals.SOCKET_PORT);
        Client.Settings.Guid = User.ID;
        Client.Settings.ConnectTimeoutSeconds = 15;
        Client.Events.ServerConnected += async (o, args) =>
        {
            await Send(new RegisterData());
        };
        Client.Events.MessageReceived += async (o, args) =>
        {
            var Ar = new GenericBufferReader(args.Data);
            var header = Ar.ReadFP<DataHeader>();

            var sender = Ar.ReadFP<UserData>();

            switch (header.DataType)
            {
                case EDataType.Ping:
                {
                    await Send(new PingData());
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

                    AppVM.Message($"From {sender.Name}", directMessageData.Text, closeTime: 5.0f);
                    break;
                }
                case EDataType.ImageHeader:
                {
                    var imageHeader = Ar.ReadFP<ImageHeaderData>();

                    IncomingImageData[sender.ID] = [imageHeader];

                    break;
                }
                case EDataType.ImageChunk:
                {
                    var imageChunk = Ar.ReadFP<ImageChunkData>();

                    IncomingImageData[sender.ID].Add(imageChunk);

                    break;
                }
                case EDataType.ImageFooter:
                {
                    var imageFooter = Ar.ReadFP<ImageFooterData>();

                    var imageHeader = IncomingImageData[sender.ID].OfType<ImageHeaderData>().First();

                    var imageChunks = IncomingImageData[sender.ID].OfType<ImageChunkData>().ToArray();
                    var imageBytes = new List<byte>();
                    for (var chunkIdx = 0; chunkIdx < imageChunks.Length; chunkIdx++)
                    {
                        var data = imageChunks[chunkIdx].Data;
                        imageBytes.AddRange(data);
                    }

                    var decompressedBytes = ZSTD_DECOMPRESS.Unwrap(imageBytes.ToArray()).ToArray();

                    unsafe
                    {
                        fixed (byte* bytePtr = decompressedBytes)
                        {
                            var intPtr = (IntPtr) bytePtr;
                            var bitmap = new WriteableBitmap(PixelFormat.Rgba8888, AlphaFormat.Unpremul, intPtr, new PixelSize(imageHeader.Width, imageHeader.Height), new Vector(96, 96), (PixelFormat.Rgba8888.BitsPerPixel * imageHeader.Width + 7) / 8);
                            ChatVM.Messages.InsertSorted(new ChatMessage
                            {
                                User = new ChatUser
                                {
                                    Name = sender.Name,
                                    Avatar = sender.AvatarURL,
                                    Id = sender.ID
                                },
                                Text = string.Empty,
                                Id = imageHeader.ID,
                                Timestamp = imageHeader.Time.ToLocalTime(),
                                ReactionCount = imageHeader.Reactions.Count,
                                ReactedTo = imageHeader.Reactions.Contains(User.ID),
                                Bitmap = bitmap,
                                BitmapName = imageHeader.Name
                            }, SortExpressionComparer<ChatMessage>.Descending(message => message.Timestamp));
                        }
                    }

                    IncomingImageData.Remove(sender.ID);

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
                    if (AppSettings.Current.Discord.Identification is not null && ChatVM is not null && !Client.Connected)
                    {
                        Client.Connect();
                    }
                }
                catch (Exception)
                {
                    
                }
            }
        });
    }


    public static void DeInit()
    {
        Client.Disconnect();
        Client.Dispose();
    }
    
    private static Decompressor ZSTD_DECOMPRESS = new();
    
    public static async Task Send(BaseData data)
    {
        var stream = new MemoryStream();
        var Ar = new BinaryWriter(stream);
        
        var header = new DataHeader(data.DataType);
        header.Serialize(Ar);
        User.Serialize(Ar);
        data.Serialize(Ar);
        
        await Client.SendAsync(stream.GetBuffer());
    }
}