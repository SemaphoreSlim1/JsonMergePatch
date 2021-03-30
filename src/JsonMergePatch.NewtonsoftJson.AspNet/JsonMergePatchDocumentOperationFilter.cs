using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace JsonMergePatch.NewtonsoftJson.AspNet
{

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
