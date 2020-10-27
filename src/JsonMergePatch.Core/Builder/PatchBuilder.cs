using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace JsonMergePatch.Core.Builder
{
    public class PatchBuilder<TTo>
    {
        private readonly IJsonMergePatch<TTo> _to;
        private readonly List<IValueApplier> _valueApplicators;

        public PatchBuilder(IJsonMergePatch<TTo> to)
        {
            if (to == null)
            { throw new ArgumentNullException(nameof(to)); }

            _to = to;
            _valueApplicators = new List<IValueApplier>();
        }

        /// <summary>
        /// Sets a value on a property
        /// </summary>
        /// <typeparam name="TToProperty">The type of the property that is receiving the value</typeparam>
        /// <param name="expr">The expression to the property that is receiving the value</param>        
        public ValueApplicator<TTo, TToProperty> Set<TToProperty>(Expression<Func<TTo, TToProperty>> expr)
        {
            var valueApplicator = new ValueApplicator<TTo, TToProperty>(_to, expr);
            _valueApplicators.Add(valueApplicator);
            return valueApplicator;
        }

        public IJsonMergePatch<TTo> Build()
        {
            foreach (var applicator in _valueApplicators)
            {
                applicator.Apply();
            }

            return _to;
        }
    }
}
