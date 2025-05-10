using System;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Models.Chat;
using FortnitePorting.Shared.Services;
using Mapster;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FortnitePorting.Models.Supabase.Tables;

[Table("messages")]
public class Message : BaseModel
{
    [PrimaryKey("id")] public string Id { get; set; }
    [Column("timestamp", ignoreOnInsert: true, ignoreOnUpdate: true)] public DateTime Timestamp { get; set; }
    [Column("user_id", ignoreOnInsert: true, ignoreOnUpdate: true)] public string UserId { get; set; }
    [Column("text")] public string Text { get; set; }
    [Column("application")] public string Application { get; set; }
    [Column("was_edited", ignoreOnInsert: true, ignoreOnUpdate: true)] public bool WasEdited { get; set; }
   
    [Column("reply_id")] public string? ReplyId { get; set; }

    static Message()
    {
        TypeAdapterConfig<Message, ChatMessageV2>.NewConfig()
            .MapToConstructor(true)
            .AfterMapping((src, dest) =>
            {
                TaskService.Run(async () =>
                {
                    var userInfo = await Api.FortnitePorting.UserInfo(src.UserId);
                    dest.User = userInfo.Adapt<ChatUserV2>();
                });
            });
    }
}