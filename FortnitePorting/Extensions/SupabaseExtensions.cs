using Newtonsoft.Json.Linq;
using Supabase.Realtime.Models;

namespace FortnitePorting.Extensions;

public static class SupabaseExtensions
{
    public static T Get<T>(this BaseBroadcast broadcast, string propertyName)
    {
        var property = broadcast.Payload![propertyName];
        if (property is JObject jsonObject)
        {
            return jsonObject.ToObject<T>()!;
        }
        return (T) property;
    }
}