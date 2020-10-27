using JsonMergePatch.Core;
using JsonMergePatch.Core.Builder;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace JsonMergePatch.NewtonsoftJson
{
    public class MergePatchDocument
    {
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

        public static IJsonMergePatch<TModel> CreateFromJson<TModel>(JObject root, JsonSerializerSettings serializerSettings = default)
        {
            serializerSettings ??= _defaultSerializerSettings.Value;
            return new MergePatchDocument<TModel>(root, serializerSettings);
        }

        public static IJsonMergePatch<TModel> CreateFromJson<TModel>(string json, JsonSerializerSettings serializerSettings = default)
        {
            var root = JObject.Parse(json);
            serializerSettings ??= _defaultSerializerSettings.Value;

            return new MergePatchDocument<TModel>(root, serializerSettings);
        }

        public static PatchBuilder<TModel> CreateBuilder<TModel>(JsonSerializerSettings serializerSettings = default)
        {
            var root = JObject.Parse("{ }");
            serializerSettings ??= _defaultSerializerSettings.Value;

            var mergePatch = new MergePatchDocument<TModel>(root, serializerSettings);
            var builder = new PatchBuilder<TModel>(mergePatch);
            return builder;
        }
    }

    public class MergePatchDocument<T> : IJsonMergePatch<T>
    {
        private readonly JObject _root;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly JsonSerializer _serializer;
        internal MergePatchDocument(JObject root, JsonSerializerSettings serializerSettings)
        {
            _root = root;
            _serializerSettings = serializerSettings;
            _serializer = JsonSerializer.Create(_serializerSettings);
        }

        public bool TryGetArray<TElement>(Expression<Func<T, IEnumerable<TElement>>> collectionExpr, out IReadOnlyList<IJsonMergePatch<TElement>> value)
        {
            value = default!;
            var found = false;
            if (TryGetValueToken(collectionExpr, forceExpand: false, out var valueToken))
            {
                found = true;

                if (valueToken is JArray jArr)
                {
                    value = jArr.Select(jt => jt as JObject).Where(jo => jo != null)
                                .Select(jo => new MergePatchDocument<TElement>(jo, _serializerSettings))
                                .ToList();
                }
            }

            return found;
        }

        public bool TryGetObject<TObjProperty>(Expression<Func<T, TObjProperty>> propertyExpr, out IJsonMergePatch<TObjProperty> value)
        {
            value = default!;
            var found = false;

            if (TryGetValueToken(propertyExpr, forceExpand: false, out var valueToken))
            {
                found = true;

                if (valueToken is JObject newRoot)
                {
                    value = new MergePatchDocument<TObjProperty>(newRoot, _serializerSettings);
                }
            }

            return found;
        }

        public bool TryGetValue<TProperty>(Expression<Func<T, TProperty>> propertyExpr, out TProperty value)
        {
            value = default!;
            var found = false;

            if (TryGetValueToken(propertyExpr, forceExpand: false, out var valueToken))
            {
                if (valueToken is JObject jobj)
                { value = (TProperty)jobj.ToObject(typeof(TProperty)); }
                else
                { value = valueToken.Value<TProperty>(); }

                found = true;
            }

            return found;
        }

        public void Set<TProperty>(Expression<Func<T, TProperty>> propertyExpr, TProperty value)
        {
            if (TryGetValueToken(propertyExpr, forceExpand: true, out var valueToken))
            {
                if (valueToken.Parent is JProperty jProp)
                {
                    if (value == null)
                    { jProp.Value = JValue.CreateNull(); }
                    else
                    { jProp.Value = JToken.FromObject(value); }
                }
            }
        }

        private bool TryGetValueToken<TProperty>(Expression<Func<T, TProperty>> propertyExpr, bool forceExpand, out JToken token)
        {
            token = default!;
            var propertyPath = GetPropertyPath(propertyExpr);
            var current = _root as JToken;
            var found = false;

            while (propertyPath.TryPop(out var propertyName))
            {
                //"current" needs to be a jobject
                var currentObj = current as JObject;
                if (currentObj == null)
                {
                    current = default(JToken);
                    found = false;
                    break;
                }

                var property = currentObj.Properties().FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

                if (property == null)
                {
                    if (forceExpand)
                    {
                        //if the property wasn't part of the graph, add it,
                        //and then push it back on the stack to retry the search
                        var newProp = new JProperty(propertyName);
                        newProp.Value = JToken.Parse("{ }");

                        currentObj.Add(newProp);
                        propertyPath.Push(propertyName);
                        continue;
                    }
                    else
                    {
                        current = default(JToken);
                        found = false;
                        break;
                    }
                }
                else
                {
                    current = property.Value;
                    found = true;
                    continue;
                }
            }

            if (found)
            { token = current; }

            return found;
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

        public T ToModel()
        {
            return _root.ToObject<T>(_serializer);
        }

        public async Task Serialize(Stream stream)
        {
            using (var sw = new StreamWriter(stream, encoding: new UTF8Encoding(false), bufferSize: 1024, leaveOpen: true))
            {
                using (var jtw = new JsonTextWriter(sw))
                {
                    await _root.WriteToAsync(jtw);
                    jtw.Flush();
                }
            }
        }
    }
}
