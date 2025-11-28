using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FortnitePorting.Models.Supabase.Tables;

[Table("errors")]
public class Error : BaseModel
{
    [PrimaryKey("id")] public string Id { get; set; }
    [Column("timestamp", ignoreOnInsert: true)] public DateTime Timestamp { get; set; }
    [Column("user_id", ignoreOnInsert: true)] public string UserId { get; set; }
    [Column("version")] public string Version { get; set; }
    [Column("message")] public string Message { get; set; }
    [Column("stack_trace")] public string StackTrace { get; set; }
}