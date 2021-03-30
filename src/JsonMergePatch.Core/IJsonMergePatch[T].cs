using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace JsonMergePatch
{
    public interface IJsonMergePatch<T>
    {
        /// <summary>
        /// Recurses through the property expression and attempts to extract the value of a property.
        /// <br />
        /// This is intended for use for actual property values, not objects with properties. Ex. Person.Address.City, not Person.Address
        /// </summary>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="propertyExpr">The expression to the property</param>
        /// <param name="value">The value of the  property</param>
        /// <returns>true, if the property value is extracted</returns>
        bool TryGetValue<TProperty>(Expression<Func<T, TProperty>> propertyExpr, out TProperty value);

        /// <summary>
        /// Recurses through the property expression and attempts to extract the value.
        /// <br />
        /// This is intended to access a nested object for further inspection, not the end property values. Ex. Person.Address, not Person.Address.City
        /// </summary>
        /// <typeparam name="TObjProperty">The type of the object to extract</typeparam>
        /// <param name="propertyExpr">The path expression to the object</param>
        /// <param name="value">The object</param>
        /// <returns>true, if the object was extracted</returns>
        bool TryGetObject<TObjProperty>(Expression<Func<T, TObjProperty>> propertyExpr, out IJsonMergePatch<TObjProperty> value);

        /// <summary>
        /// Recurses through the property expression and attempts to extract the collection
        /// </summary>
        /// <typeparam name="TElement">The type of the elements in the collection</typeparam>
        /// <param name="collectionExpr">The path to the collection</param>
        /// <param name="value">The resolved collection</param>
        /// <returns>true, if the object was extracted</returns>
        public bool TryGetArray<TElement>(Expression<Func<T, IEnumerable<TElement>>> collectionExpr, out IReadOnlyList<IJsonMergePatch<TElement>> value);

        /// <summary>
        /// Get this merge patch, fully deserialized
        /// </summary>        
        T ToModel();
    }
}
