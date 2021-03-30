using System;
using System.Diagnostics;
using System.Text.Json;

namespace JsonMergePatch.SystemText
{
    public class JsonMergePatch
    {
        public const string ContentType = "application/merge-patch+json";

        private static readonly Lazy<JsonSerializerOptions> _defaultSerializerOptions = new Lazy<JsonSerializerOptions>(() =>
        {
            var opts = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = Debugger.IsAttached
            };

            opts.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            return opts;
        });

        public static IJsonMergePatch<TModel> Create<TModel>(JsonDocument json, JsonSerializerOptions serializerOptions = default)
        {
            serializerOptions ??= _defaultSerializerOptions.Value;

            return new SystemTextJsonMergePatch<TModel>(json, serializerOptions);
        }

        public static IJsonMergePatch<TModel> CreateFromJson<TModel>(string json, JsonDocumentOptions documentOptions = default, JsonSerializerOptions serializerOptions = default)
        {
            var doc = JsonDocument.Parse(json, documentOptions);
            serializerOptions ??= _defaultSerializerOptions.Value;

            return new SystemTextJsonMergePatch<TModel>(doc, serializerOptions);
        }
    }
}
