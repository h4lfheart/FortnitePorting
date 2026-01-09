using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Supabase;
using Supabase.Realtime.Models;

namespace FortnitePorting.Extensions;

public static class SupabaseExtensions
{
    extension(BaseBroadcast broadcast)
    {
        public T Get<T>(string propertyName)
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
        
        public T[] GetArray<T>(string propertyName)
        {
            var property = broadcast.Payload![propertyName];
            if (property is not JArray jsonArray)
                return [];

            var tokens = jsonArray.ToArray();
            
            return [..tokens
                .Select(token => token.Type == JTokenType.Object ? token.ToObject<T>() : token.Value<T>())
                .Where(item => item is not null)!
            ];
        }
    }

    extension(Client client)
    {
        public async Task<T[]> CallTableFunction<T>(string name, object? args = null)
        {
            return await client.Rpc<T[]>(name, args ?? new { }) ?? [];
        }
        
        public async Task<T?> CallPrimitiveFunction<T>(string name, object? args = null)
        {
            return await client.Rpc<T>(name, args ?? new { }) ?? default;
        }
        
        public async Task<T?> CallObjectFunction<T>(string name, object? args = null)
        {
            var result = await client.Rpc<T[]>(name, args ?? new { });
            return result is null ? default : result.FirstOrDefault();
        }
    }
    
}
