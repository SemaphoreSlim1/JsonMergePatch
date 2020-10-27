using System;

namespace JsonMergePatch.Core.Builder
{
    public class Conditional<TFrom, TFromProperty, TToProperty>
    {
        private readonly ValueInterceptor<TFrom, TFromProperty, TToProperty> _owner;

        public Conditional(ValueInterceptor<TFrom, TFromProperty, TToProperty> owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Ensures that the destination value will be set to the source value only if the source is not null or whitespace
        /// </summary>
        public IValueInterceptor<TFrom, TFromProperty, TToProperty> NotNullOrWhiteSpace()
        {
            var predicate = new Func<object, bool>(input => string.IsNullOrWhiteSpace(input?.ToString()) == false);
            _owner.SetConditionalPredicate(predicate);
            return _owner;
        }

        /// <summary>
        /// Ensures that the destination value will be set to the source value only if the source value is not null
        /// </summary>
        public IValueInterceptor<TFrom, TFromProperty, TToProperty> NotNull()
        {
            var predicate = new Func<object, bool>(input => input != null);
            _owner.SetConditionalPredicate(predicate);
            return _owner;
        }
    }
}
