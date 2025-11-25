using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Bookify.Application.Helpers
{
    public static class SessionHelper
    {
        //for storing JSON objects in ISession.
        public static void SetObjectAsJson<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            if (string.IsNullOrEmpty(json))
                return default;
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}
