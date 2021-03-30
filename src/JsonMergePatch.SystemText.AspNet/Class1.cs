using System;

namespace JsonMergePatch.SystemText.AspNet
{
    public class NewtonsoftJsonMergePatchOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ArrayPool<char> _charPool;
        private readonly ObjectPoolProvider _objectPoolProvider;
        private readonly IOptions<MvcNewtonsoftJsonOptions> _jsonOptions;
        private readonly Lazy<IModelMetadataProvider> _modelMetadataProvider;

        public NewtonsoftJsonMergePatchOptionsSetup(
            ILoggerFactory loggerFactory,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider,
            Lazy<IModelMetadataProvider> modelMetadataProvider,
            IOptions<MvcNewtonsoftJsonOptions> jsonOptions)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _charPool = charPool ?? throw new ArgumentNullException(nameof(charPool));
            _objectPoolProvider = objectPoolProvider ?? throw new ArgumentNullException(nameof(objectPoolProvider));
            _modelMetadataProvider = modelMetadataProvider ?? throw new ArgumentNullException(nameof(modelMetadataProvider));
            _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
        }

        public void Configure(MvcOptions mvcOptions)
        {
            var jsonMergePatchLogger = _loggerFactory.CreateLogger<MergePatchInputFormatter>();
            mvcOptions.InputFormatters.Insert(0, new MergePatchInputFormatter(jsonMergePatchLogger,
                                                                              _jsonOptions.Value.SerializerSettings,
                                                                              _charPool,
                                                                              _objectPoolProvider,
                                                                              mvcOptions,
                                                                              _jsonOptions.Value,
                                                                              _modelMetadataProvider));
        }
    }


    public static class MvcBuilderExtensions
    {
        public static IMvcBuilder AddNewtonsoftJsonMergePatch(this IMvcBuilder builder)
        {
            builder.Services.AddTransient<IConfigureOptions<MvcOptions>, NewtonsoftJsonMergePatchOptionsSetup>();
            builder.Services.AddSingleton<Lazy<IModelMetadataProvider>>(sp => new Lazy<IModelMetadataProvider>(() => sp.GetRequiredService<IModelMetadataProvider>()));
            return builder;
        }

    }

    internal class MergePatchInputFormatter : SystemTextJsonInputFormatter
    {
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
                var createMethod = typeof(JsonMergePatch).GetMethod(nameof(JsonMergePatch.Create))
                                                             .MakeGenericMethod(new[] { context.ModelType.GenericTypeArguments[0] });

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

    public class JsonMergePatchDocumentOperationFilter : IOperationFilter
    {

        private static bool IsJsonMergePatchDocumentType(Type t) => (t != null) && t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IJsonMergePatch<>));

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            OpenApiSchema GenerateSchema(Type type)
                => context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

            void CleanUpSchemas(string jsonMergePatchSchemaId)
            {
                var schemas = context.SchemaRepository.Schemas;
                if (schemas.TryGetValue(jsonMergePatchSchemaId, out var jsonMergePatchSchema))
                {
                    //var contractResolverSchema = jsonMergePatchSchema.Properties["contractResolver"];
                    //var operationsSchema = jsonMergePatchSchema.Properties["operations"];
                    //schemas.Remove(jsonMergePatchSchemaId);
                    //schemas.Remove(contractResolverSchema.AllOf.Single().Reference.Id);
                    //schemas.Remove(operationsSchema.Items.Reference.Id);
                }
            }

            var bodyParameters = context.ApiDescription.ParameterDescriptions.Where(p => p.Source == BindingSource.Body).ToList();

            foreach (var parameter in bodyParameters)
            {
                if (IsJsonMergePatchDocumentType(parameter.Type))
                {
                    CleanUpSchemas(operation.RequestBody.Content[JsonMergePatch.ContentType].Schema.Reference.Id);
                    operation.RequestBody.Content[JsonMergePatch.ContentType].Schema = GenerateSchema(parameter.Type.GenericTypeArguments[0]);
                }
            }
        }


    }
}
