using System;

namespace JsonMergePatch.Builder
{
    public interface IValueResolver<T>
    {
        void SetValueResolver(Func<(bool ShouldApply, T Value)> resolver);
    }
}
