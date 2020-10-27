using System;

namespace JsonMergePatch.Core.Builder
{
    public class ValueInterceptor<TFrom, TFromProperty, TToProperty> : IValueInterceptor<TFrom, TFromProperty, TToProperty>
    {
        private readonly ValueReader<TFrom, TToProperty> _owner;

        public ValueInterceptor(ValueReader<TFrom, TToProperty> owner)
        {
            _owner = owner;
        }

        public IValueInterceptor<TFrom, TFromProperty, TToProperty> OnlyIf(Func<TFromProperty, bool> predicate)
        {
            var pred = new Func<object, bool>(input =>
            {
                var from = (TFromProperty)input;
                var result = predicate(from);
                return result;
            });

            _owner.SetConditional(pred);
            return this;
        }

        public Conditional<TFrom, TFromProperty, TToProperty> OnlyIf()
        {
            return new Conditional<TFrom, TFromProperty, TToProperty>(this);
        }

        public IValueInterceptor<TFrom, TFromProperty, TToProperty> UsingConversion(Func<TFromProperty, TToProperty> converter)
        {
            var convertFunc = new Func<object, TToProperty>(input =>
            {
                var tFrom = (TFromProperty)input;
                var result = converter(tFrom);
                return result;
            });

            _owner.SetConverter(convertFunc);
            return this;
        }

        public void SetConditionalPredicate(Func<object, bool> condition)
        {
            _owner.SetConditional(condition);
        }
    }
}
