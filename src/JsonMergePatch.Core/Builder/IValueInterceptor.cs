using System;

namespace JsonMergePatch.Core.Builder
{
    public interface IValueInterceptor<TFrom, TFromProperty, TToProperty>
    {
        IValueInterceptor<TFrom, TFromProperty, TToProperty> OnlyIf(Func<TFromProperty, bool> predicate);
        Conditional<TFrom, TFromProperty, TToProperty> OnlyIf();
        IValueInterceptor<TFrom, TFromProperty, TToProperty> UsingConversion(Func<TFromProperty, TToProperty> converter);
    }
}
