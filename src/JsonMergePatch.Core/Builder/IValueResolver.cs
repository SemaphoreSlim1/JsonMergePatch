using System;

namespace JsonMergePatch.Core.Builder
{
    public interface IValueResolver<T>
    {
        void SetValueResolver(Func<(bool ShouldApply, T Value)> resolver);
    }
}
