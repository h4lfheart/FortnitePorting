using System;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Models.Chat;
using FortnitePorting.Services;
using Mapster;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FortnitePorting.Models.Supabase.Tables;

[Table("messages")]
public class Message : BaseModel
{
    [JsonProperty("id")] 
    [PrimaryKey("id")] 
    public string Id { get; set; }
    
    [JsonProperty("timestamp")] 
    [Column("timestamp", ignoreOnInsert: true, ignoreOnUpdate: true)] 
    public DateTime Timestamp { get; set; }
    
    [JsonProperty("user_id")] 
    [Column("user_id", ignoreOnInsert: true, ignoreOnUpdate: true)] 
    public string UserId { get; set; }
    
    [JsonProperty("text")] 
    [Column("text")] 
    public string Text { get; set; }
    
    [JsonProperty("application")] 
    [Column("application")] 
    public string Application { get; set; }
    
    [JsonProperty("was_edited")]
    [Column("was_edited", ignoreOnInsert: true, ignoreOnUpdate: true)] 
    public bool WasEdited { get; set; }
    
    [JsonProperty("reply_id")]
    [Column("reply_id")]
    public string? ReplyId { get; set; }
    
    [JsonProperty("image_path")] 
    [Column("image_path")] 
    public string? ImagePath { get; set; }

    [JsonProperty("reactor_ids")]
    [Column("reactor_ids", ignoreOnInsert: true,  ignoreOnUpdate: true)]
    public string[] ReactorIds { get; set; } = [];

    static Message()
    {
        TypeAdapterConfig<Message, ChatMessageV2>.NewConfig()
            .MapToConstructor(true)
            .AfterMapping((src, dest) =>
            {
                TaskService.Run(async () =>
                {
                    dest.User = await AppServices.Chat.GetUser(src.UserId);
                });
            });
    }
}