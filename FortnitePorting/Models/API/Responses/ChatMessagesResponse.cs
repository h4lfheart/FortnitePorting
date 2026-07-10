using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public class ChatMessagesResponse
{
    [JsonProperty("messages")] public List<ChatMessageEntry> Messages { get; set; } = [];
    [JsonProperty("nextCursor")] public DateTime? NextCursor { get; set; }
}

public class UploadImageResponse
{
    [JsonProperty("path")] public string Path { get; set; } = string.Empty;
}

public class ChatMessageEntry
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("userId")] public string UserId { get; set; }
    [JsonProperty("text")] public string Text { get; set; }
    [JsonProperty("application")] public string Application { get; set; }
    [JsonProperty("replyId")] public string? ReplyId { get; set; }
    [JsonProperty("imagePath")] public string? ImagePath { get; set; }
    [JsonProperty("gameFilePath")] public string? GameFilePath { get; set; }
    [JsonProperty("reactorIds")] public string[] ReactorIds { get; set; } = [];
    [JsonProperty("wasEdited")] public bool WasEdited { get; set; }
    [JsonProperty("createdAt")] public DateTime CreatedAt { get; set; }
}
