using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Supabase;
using Supabase.Realtime.Models;

namespace FortnitePorting.Extensions;

public static class SupabaseExtensions
{
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter() }
    };

    extension(BaseBroadcast broadcast)
    {
        public T Get<T>(string propertyName)
        {
            var value = broadcast.Payload?.GetValueOrDefault(propertyName);
            if (value is null) return default!;
            if (value is T typedValue) return typedValue;

            var json = JsonSerializer.Serialize(value, PayloadSerializerOptions);
            return JsonSerializer.Deserialize<T>(json, PayloadSerializerOptions)!;
        }

        public T[] GetArray<T>(string propertyName)
        {
            return broadcast.Get<T[]>(propertyName) ?? [];
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
