using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace JsonMergePatch.NewtonsoftJson.AspNet
{
    public static class MvcBuilderExtensions
    {
        public static IMvcBuilder AddNewtonsoftJsonMergePatch(this IMvcBuilder builder)
        {
            builder.Services.AddTransient<IConfigureOptions<MvcOptions>, NewtonsoftJsonMergePatchOptionsSetup>();
            builder.Services.AddSingleton<Lazy<IModelMetadataProvider>>(sp => new Lazy<IModelMetadataProvider>(() => sp.GetRequiredService<IModelMetadataProvider>()));
            return builder;
        }

    }
}
