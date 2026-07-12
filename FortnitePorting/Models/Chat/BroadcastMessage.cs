using System;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Chat;

public class BroadcastMessage
{
    [JsonProperty("id")] public string Id { get; set; } = string.Empty;
    [JsonProperty("user_id")] public string UserId { get; set; } = string.Empty;
    [JsonProperty("timestamp")] public DateTime Timestamp { get; set; }
    [JsonProperty("text")] public string Text { get; set; } = string.Empty;
    [JsonProperty("application")] public string Application { get; set; } = string.Empty;
    [JsonProperty("was_edited")] public bool WasEdited { get; set; }
    [JsonProperty("reply_id")] public string? ReplyId { get; set; }
    [JsonProperty("image_path")] public string? ImagePath { get; set; }
    [JsonProperty("game_file_path")] public string? GameFilePath { get; set; }
    [JsonProperty("reactor_ids")] public string[] ReactorIds { get; set; } = [];
}
