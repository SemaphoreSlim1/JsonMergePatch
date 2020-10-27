using System;
using System.Linq.Expressions;

namespace JsonMergePatch.Core.Builder
{
    /// <summary>
    /// Applies a value to a property on a poco
    /// </summary>
    /// <typeparam name="TTo">The type of the object receiveing the value</typeparam>
    /// <typeparam name="TToProperty">The type of the property receiving the value</typeparam>
    public class ValueApplicator<TTo, TToProperty> : IValueApplicator<TTo, TToProperty>, IValueApplier, IValueResolver<TToProperty>
    {
        protected readonly IJsonMergePatch<TTo> _to;
        protected readonly Expression<Func<TTo, TToProperty>> _toExpr;

        protected Func<(bool ShouldApply, TToProperty Value)> _valueResolver;

        /// <summary>
        /// Creates a new value applicator
        /// </summary>
        /// <param name="to">The object to apply the value to</param>
        /// <param name="toExpr">The expression that accessess the property being set</param>
        public ValueApplicator(IJsonMergePatch<TTo> to, Expression<Func<TTo, TToProperty>> toExpr)
        {
            _to = to;
            _toExpr = toExpr;
        }

        public IValueReader<TFrom, TToProperty> To<TFrom>(IJsonMergePatch<TFrom> from)
        {
            return new ValueReader<TFrom, TToProperty>(from, this);
        }

        /// <summary>
        /// Sets the property to the specified value using the resolver func
        /// </summary>            
        public void ToValue(Func<TToProperty> resolver)
        {
            if (resolver == null)
            { throw new NullReferenceException(); }

            _valueResolver = new Func<(bool ShouldApply, TToProperty Value)>(() =>
            {
                var value = resolver();
                return (ShouldApply: true, Value: value);
            });
        }

        /// <summary>
        /// Sets the property to the specified value
        /// </summary>
        /// <param name="value"></param>
        public void ToValue(TToProperty value)
        {
            _valueResolver = new Func<(bool ShouldApply, TToProperty Value)>(() =>
            {
                return (ShouldApply: true, Value: value);
            });
        }

        public void SetValueResolver(Func<(bool ShouldApply, TToProperty Value)> retriever)
        {
            _valueResolver = retriever;
        }

        public void Apply()
        {
            if (_valueResolver == null)
            { return; }

            var result = _valueResolver();
            if (result.ShouldApply)
            {
                _to.Set(_toExpr, result.Value);
            }
        }
    }
}
