using System;
using System.Linq.Expressions;

namespace JsonMergePatch.Builder
{
    public class ValueReader<TFrom, TToProperty> : IValueReader<TFrom, TToProperty>
    {
        protected readonly IJsonMergePatch<TFrom> _from;
        protected readonly IValueResolver<TToProperty> _valueResolver;

        private Func<object, bool> _conditional;
        private Func<object, TToProperty> _converter;

        public ValueReader(IJsonMergePatch<TFrom> from, IValueResolver<TToProperty> valueResolver)
        {
            _from = from;
            _valueResolver = valueResolver;
        }

        public void SetConditional(Func<object, bool> conditional)
        { _conditional = conditional; }

        public void SetConverter(Func<object, TToProperty> converter)
        { _converter = converter; }


        public IValueInterceptor<TFrom, TFromProperty, TToProperty> Property<TFromProperty>(Expression<Func<TFrom, TFromProperty>> fromExpr)
        {
            var resolver = new Func<(bool ShouldApply, TToProperty Value)>(() =>
            {
                //pass through for converter and conditional, unless explicitly set
                _conditional = _conditional ?? new Func<object, bool>(input => true);
                _converter = _converter ?? new Func<object, TToProperty>(input => (TToProperty)input);

                var fromValue = default(TFromProperty);
                var shouldApply = false;

                var toValue = default(TToProperty);
                if (_from?.TryGetValue(fromExpr, out fromValue) ?? false)
                {
                    shouldApply = _conditional(fromValue);
                    if (shouldApply)
                    { toValue = _converter(fromValue); }
                }

                return (ShouldApply: shouldApply, Value: toValue);
            });

            _valueResolver.SetValueResolver(resolver);

            var interceptor = new ValueInterceptor<TFrom, TFromProperty, TToProperty>(this);
            return interceptor;
        }
    }
}
