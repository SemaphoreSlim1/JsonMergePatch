using JsonMergePatch.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonMergePatch.SystemText
{
    public class MergePatchDocument
    {
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

        public static IJsonMergePatch<TModel> CreateFromJson<TModel>(JsonDocument json, JsonSerializerOptions serializerOptions = default)
        {
            serializerOptions ??= _defaultSerializerOptions.Value;

            return new MergePatchDocument<TModel>(json, serializerOptions);
        }

        public static IJsonMergePatch<TModel> CreateFromJson<TModel>(string json, JsonDocumentOptions documentOptions = default, JsonSerializerOptions serializerOptions = default)
        {
            var doc = JsonDocument.Parse(json, documentOptions);
            serializerOptions ??= _defaultSerializerOptions.Value;

            return new MergePatchDocument<TModel>(doc, serializerOptions);
        }
    }

    public class MergePatchDocument<T> : IJsonMergePatch<T>
    {
        private readonly JsonDocument _json;
        private readonly JsonSerializerOptions _serializerOptions;

        internal MergePatchDocument(JsonDocument json, JsonSerializerOptions serializerOptions)
        {
            _json = json;
            _serializerOptions = serializerOptions;
        }

        /// <summary>
        /// Recurses through the property expression and attempts to extract the value of a property.
        /// <br />
        /// This is intended for use for actual property values, not objects with properties. Ex. Person.Address.City, not Person.Address
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertyExpr">The expression to the property</param>
        /// <param name="value">The value of the  property</param>
        /// <returns>true, if the property value is extracted</returns>
        public bool TryGetValue<TProperty>(Expression<Func<T, TProperty>> propertyExpr, out TProperty value)
        {
            value = default!;

            if (TryGetElement(propertyExpr, out var jsonElement) == false)
            { return false; }

            var bufferWriter = new ArrayBufferWriter<byte>();

            using (var writer = new Utf8JsonWriter(bufferWriter))
            { jsonElement.WriteTo(writer); }

            value = JsonSerializer.Deserialize<TProperty>(bufferWriter.WrittenSpan, _serializerOptions);
            return true;
        }

        /// <summary>
        /// Recurses through the property expression and attempts to extract the value.
        /// <br />
        /// This is intended to access a nested object for further inspection, not the end property values. Ex. Person.Address, not Person.Address.City
        /// </summary>
        /// <typeparam name="TObjProperty">The type of the object to extract</typeparam>
        /// <param name="propertyExpr">The path expression to the object</param>
        /// <param name="value">The object</param>
        /// <returns>true, if the object was extracted</returns>
        public bool TryGetObject<TObjProperty>(Expression<Func<T, TObjProperty>> propertyExpr, out IJsonMergePatch<TObjProperty> value)
        {
            value = default!;

            if (TryGetElement(propertyExpr, out var jsonElement) == false)
            { return false; }

            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                var bufferWriter = new ArrayBufferWriter<byte>();

                using (var writer = new Utf8JsonWriter(bufferWriter))
                { jsonElement.WriteTo(writer); }

                var childDoc = JsonDocument.Parse(bufferWriter.WrittenMemory);
                value = new MergePatchDocument<TObjProperty>(childDoc, _serializerOptions);

                return true;
            }
            else
            {
                value = null;
                return true;
            }
        }

        /// <summary>
        /// Recurses through the property expression and attempts to extract the collection
        /// </summary>
        /// <typeparam name="TElement">The type of the elements in the collection</typeparam>
        /// <param name="collectionExpr">The path to the collection</param>
        /// <param name="value">The resolved collection</param>
        /// <returns>true, if the object was extracted</returns>
        public bool TryGetArray<TElement>(Expression<Func<T, IEnumerable<TElement>>> collectionExpr, out IReadOnlyList<IJsonMergePatch<TElement>> value)
        {
            value = default!;

            if (TryGetElement(collectionExpr, out var jsonElement) == false)
            { return false; }


            if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                value = jsonElement.EnumerateArray().Select(je =>
                {

                    var bufferWriter = new ArrayBufferWriter<byte>();

                    using (var writer = new Utf8JsonWriter(bufferWriter))
                    { jsonElement.WriteTo(writer); }

                    var childDoc = JsonDocument.Parse(bufferWriter.WrittenMemory);
                    return new MergePatchDocument<TElement>(childDoc, _serializerOptions);
                }).ToList();

                return true;
            }
            else
            {
                value = null;
                return true;
            }
        }

        private bool TryGetElement<TProperty>(Expression<Func<T, TProperty>> propertyExpr, out JsonElement jsonElement)
        {
            var propertyPath = GetPropertyPath(propertyExpr);
            jsonElement = _json.RootElement;
            var found = false;

            while (propertyPath.TryPop(out var propertyName))
            {
                if (jsonElement.TryGetProperty(propertyName, out var childElement))
                {
                    jsonElement = childElement;
                    found = true;
                    continue;
                }
                else if (jsonElement.TryGetProperty(CamelCase(propertyName), out childElement))
                {
                    jsonElement = childElement;
                    found = true;
                    continue;
                }

                jsonElement = default!;
                found = false;
                break;
            }

            return found;
        }


        public void Set<TProperty>(Expression<Func<T, TProperty>> propertyExpr, TProperty value)
        {
            //support for setting values using system.text.json is not supported at this time
            throw new NotSupportedException();

            /*
            var propertyPath = GetPropertyPath(propertyExpr);
            var jsonElement = _json.RootElement;

            while (propertyPath.TryPop(out var propertyName))
            {
                if (jsonElement.TryGetProperty(propertyName, out var childElement))
                {
                    jsonElement = childElement;
                    //found = true;
                    continue;
                }
                else if (jsonElement.TryGetProperty(CamelCase(propertyName), out childElement))
                {
                    jsonElement = childElement;
                    //found = true;
                    continue;
                }
                else
                {

                    jsonElement = jsonElement.AddProperty(propertyName, value);
                }
            }
            */
        }

        private Stack<string> GetPropertyPath<TProperty>(Expression<Func<T, TProperty>> propertyExpr)
        {
            var memberExpression = propertyExpr.Body as MemberExpression;
            var names = new Stack<string>();

            while (memberExpression != null)
            {
                names.Push(memberExpression.Member.Name);
                memberExpression = memberExpression.Expression as MemberExpression;
            }

            return names;
        }

        private static string CamelCase(string name)
        {
            if (name == null || name.Length == 0)
            { return name; }

            if (name.Length == 1)
            { return name.ToLower(); }

            return char.ToLower(name[0]) + name.Substring(1);
        }

        public T ToModel()
        {
            var bufferWriter = new ArrayBufferWriter<byte>();

            using (var writer = new Utf8JsonWriter(bufferWriter))
            { _json.RootElement.WriteTo(writer); }

            return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, _serializerOptions);
        }

        public async Task Serialize(Stream stream)
        {
            throw new NotSupportedException();
        }
    }
}
