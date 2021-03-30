using System;
using System.Linq.Expressions;

namespace JsonMergePatch.Builder
{
    public interface IValueReader<TFrom, TToProperty>
    {
        IValueInterceptor<TFrom, TFromProperty, TToProperty> Property<TFromProperty>(Expression<Func<TFrom, TFromProperty>> fromExpr);
    }
}
