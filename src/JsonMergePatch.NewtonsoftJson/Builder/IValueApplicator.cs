using System;

namespace JsonMergePatch.Builder
{
    public interface IValueApplicator<TTo, TToProperty>
    {
        IValueReader<TFrom, TToProperty> To<TFrom>(IJsonMergePatch<TFrom> from);

        /// <summary>
        /// Sets the property to the specified value using the resolver func
        /// </summary>            
        void ToValue(Func<TToProperty> resolver);

        /// <summary>
        /// Sets the property to the specified value
        /// </summary>
        /// <param name="value"></param>
        void ToValue(TToProperty value);
    }
}
