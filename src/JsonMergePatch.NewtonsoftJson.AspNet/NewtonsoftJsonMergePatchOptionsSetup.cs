using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;

namespace JsonMergePatch.NewtonsoftJson.AspNet
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
            IOptions<MvcNewtonsoftJsonOptions> jsonOptions,
            Lazy<IModelMetadataProvider> modelMetadataProvider)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _charPool = charPool ?? throw new ArgumentNullException(nameof(charPool));
            _objectPoolProvider = objectPoolProvider ?? throw new ArgumentNullException(nameof(objectPoolProvider));
            _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
            _modelMetadataProvider = modelMetadataProvider ?? throw new ArgumentNullException(nameof(modelMetadataProvider));
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
}
