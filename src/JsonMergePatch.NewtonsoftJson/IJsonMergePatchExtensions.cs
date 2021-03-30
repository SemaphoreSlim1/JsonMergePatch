using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json.Linq;
using System;
using System.Linq.Expressions;

namespace JsonMergePatch
{
    public static class IJsonMergePatchExtensions
    {
        private static string replaceOp = OperationType.Replace.ToString();
        private static string addOp = OperationType.Add.ToString();
        private static string removeOp = OperationType.Remove.ToString();

        /// <summary>
        /// Sets a property to be a value, forcefully expanding if necessary
        /// </summary>
        /// <typeparam name="TProperty">The type of the property to set</typeparam>
        /// <param name="propertyExpr">The expression path to the property</param>
        /// <param name="value">The value to set</param>
        public static void Set<TModel, TProperty>(this IJsonMergePatch<TModel> mergePatch, Expression<Func<TModel, TProperty>> propertyExpr, TProperty value)
        {
            if (mergePatch is NewtonsoftJson.NewtonsoftJsonMergePatch<TModel> mp)
            {
                if (mp.TryGetValueToken(propertyExpr, forceExpand: true, out var valueToken))
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
            else
            {
                throw new InvalidOperationException($"This operation is only supported on {nameof(NewtonsoftJson.NewtonsoftJsonMergePatch<TModel>)}");
            }
        }

        public static Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> ToJsonPatch<TModel>(this IJsonMergePatch<TModel> mergePatch, JsonMergePatchOptions options = null)
            where TModel : class
        {
            options = options ?? JsonMergePatchOptions.Default;

            if (mergePatch is NewtonsoftJson.NewtonsoftJsonMergePatch<TModel> mp)
            {
                var pd = new Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel>();
                AddOperation(pd, "/", mp._root, options);
                return pd;
            }

            throw new InvalidOperationException($"This operation is only supported on {nameof(NewtonsoftJson.NewtonsoftJsonMergePatch<TModel>)}");
        }

        private static void AddOperation<TModel>(Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> doc, string pathPrefix, JObject mergePatch, JsonMergePatchOptions options)
            where TModel : class
        {
            foreach (var jProperty in mergePatch)
            {
                // Encode any possible "/" in the path. Ref: https://tools.ietf.org/html/rfc6901#section-3
                var path = pathPrefix + jProperty.Key.Replace("/", "~1");

                if (jProperty.Value is JValue jValue)
                {
                    if (options.EnableDelete && jValue.Value == null)
                    { doc.AddOperation_Remove(path); }
                    else
                    { doc.AddOperation_Replace(path, jValue.Value); }
                }
                else if (jProperty.Value is JArray jArray)
                { doc.AddOperation_Replace(path, jArray); }
                else if (jProperty.Value is JObject jObj)
                {
                    doc.AddOperation_Add(path);
                    AddOperation(doc, path + "/", jObj, options);
                }
            }
        }

        private static void AddOperation_Replace<TModel>(this Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> doc, string path, object value)
            where TModel : class
        { doc.Operations.Add(new Operation<TModel>(replaceOp, path, null, value)); }

        private static void AddOperation_Remove<TModel>(this Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> doc, string path)
            where TModel : class
        { doc.Operations.Add(new Operation<TModel>(removeOp, path, null, null)); }

        private static void AddOperation_Add<TModel>(this Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<TModel> doc, string path)
            where TModel : class
        {
            var propertyType = NewtonsoftJson.ReflectionHelper.GetPropertyTypeFromPath(typeof(TModel), path, doc.ContractResolver);
            doc.Operations.Add(new Operation<TModel>(addOp, path, null, doc.ContractResolver.ResolveContract(propertyType).DefaultCreator()));
        }
    }
}
