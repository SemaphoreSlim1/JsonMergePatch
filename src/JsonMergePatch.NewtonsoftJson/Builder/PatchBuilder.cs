using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace JsonMergePatch.Builder
{
    public class PatchBuilder<TTo>
    {
        private readonly List<IValueApplier<TTo>> _valueApplicators;

        public PatchBuilder()
        {
            _valueApplicators = new List<IValueApplier<TTo>>();
        }

        /// <summary>
        /// Sets a value on a property
        /// </summary>
        /// <typeparam name="TToProperty">The type of the property that is receiving the value</typeparam>
        /// <param name="expr">The expression to the property that is receiving the value</param>        
        public ValueApplicator<TTo, TToProperty> Set<TToProperty>(Expression<Func<TTo, TToProperty>> expr)
        {
            var valueApplicator = new ValueApplicator<TTo, TToProperty>(expr);
            _valueApplicators.Add(valueApplicator);
            return valueApplicator;
        }

        public void ApplyTo(IJsonMergePatch<TTo> mergePatch)
        {
            foreach (var applicator in _valueApplicators)
            {
                applicator.Apply(mergePatch);
            }
        }
    }
}
