namespace JsonMergePatch.Builder
{
    public interface IValueApplier<TModel>
    {
        void Apply(IJsonMergePatch<TModel> mergePatch);
    }
}
