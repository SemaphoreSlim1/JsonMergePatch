using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JsonMergePatch.NewtonsoftJson.AspNet
{
    internal class MergePatchInputFormatter : NewtonsoftJsonInputFormatter
    {
        private static ConcurrentDictionary<Type, MethodInfo> _patchCreators = new ConcurrentDictionary<Type, MethodInfo>();

        private readonly Lazy<ModelMetadata> _modelMetadata;

        private static bool ContainerIsIEnumerable(InputFormatterContext context)
            => context.ModelType.IsGenericType && (context.ModelType.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        public MergePatchInputFormatter(ILogger logger,
                                        JsonSerializerSettings serializerSettings,
                                        ArrayPool<char> charPool,
                                        ObjectPoolProvider objectPoolProvider,
                                        MvcOptions options,
                                        MvcNewtonsoftJsonOptions jsonOptions,
                                        Lazy<IModelMetadataProvider> modelMetadataProvider)
            : base(logger, serializerSettings, charPool, objectPoolProvider, options, jsonOptions)
        {
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add(Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(JsonMergePatch.ContentType));

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);

            _modelMetadata = new Lazy<ModelMetadata>(() => modelMetadataProvider.Value.GetMetadataForType(typeof(JObject)));
        }

        public override bool CanRead(InputFormatterContext context)
        {
            if (context == null)
            { throw new ArgumentNullException(nameof(context)); }

            var mergePatchType = context.ModelType;

            if (ContainerIsIEnumerable(context))
            {
                mergePatchType = context.ModelType.GenericTypeArguments[0];
            }

            return mergePatchType.IsGenericType && (mergePatchType.GetGenericTypeDefinition() == typeof(IJsonMergePatch<>));
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            var patchContext = new InputFormatterContext(
                context.HttpContext,
                context.ModelName,
                context.ModelState,
                _modelMetadata.Value,
                context.ReaderFactory,
                context.TreatEmptyInputAsDefaultValue);

            var jsonResult = await base.ReadRequestBodyAsync(patchContext);

            if (jsonResult.HasError)
            { return jsonResult; }

            var serializer = base.CreateJsonSerializer();

            try
            {
                var model = jsonResult.Model;
                var modelType = context.ModelType.GenericTypeArguments[0];
                var createMethod = _patchCreators.GetOrAdd(modelType, (mt) => typeof(JsonMergePatch).GetMethod(nameof(JsonMergePatch.Create)).MakeGenericMethod(new[] { mt }));

                var patch = createMethod.Invoke(null, new object[] { model, SerializerSettings });
                return await InputFormatterResult.SuccessAsync(patch);
            }
            catch
            {
                //log
                return await InputFormatterResult.FailureAsync();
            }
            finally
            {
                base.ReleaseJsonSerializer(serializer);
            }
        }
    }
}
