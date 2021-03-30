using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;

namespace JsonMergePatch.NewtonsoftJson
{
    public class JsonMergePatch
    {
        public const string ContentType = "application/merge-patch+json";

        private static readonly Lazy<JsonSerializerSettings> _defaultSerializerSettings = new Lazy<JsonSerializerSettings>(() =>
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Debugger.IsAttached ? Formatting.Indented : Formatting.None,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            };

            settings.Converters.Add(new StringEnumConverter());

            return settings;
        });


        public static IJsonMergePatch<TModel> Create<TModel>(JObject root, JsonSerializerSettings serializerSettings = default)
        {
            serializerSettings ??= _defaultSerializerSettings.Value;
            return new NewtonsoftJsonMergePatch<TModel>(root, serializerSettings);
        }

        public static IJsonMergePatch<TModel> New<TModel>(JsonSerializerSettings serializerSettings = default)
        {
            return CreateFromJson<TModel>("{ }", serializerSettings);
        }

        public static IJsonMergePatch<TModel> CreateFromJson<TModel>(string json, JsonSerializerSettings serializerSettings = default)
        {
            serializerSettings ??= _defaultSerializerSettings.Value;
            var root = JObject.Parse(json);
            return new NewtonsoftJsonMergePatch<TModel>(root, serializerSettings);
        }
    }
}
