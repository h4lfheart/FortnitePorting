using System;
using System.Linq;
using FortnitePorting.Models.Supabase.Tables;
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

        if (property is string stringValue && typeof(T).IsAssignableTo(typeof(Enum)))
        {
            var enumValues = (T[]) Enum.GetValues(typeof(T));
            return enumValues.First(enumValue => enumValue!.ToString()!.Equals(stringValue, StringComparison.OrdinalIgnoreCase));
        }
        
        return (T) property;
    }
}
