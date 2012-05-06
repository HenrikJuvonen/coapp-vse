using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Versioning;
using CoApp.Toolkit.Engine.Client;

namespace CoGet
{
    public static class PackageExtensions
    {
        private const string TagsProperty = "Tags";
        private static readonly string[] _packagePropertiesToSearch = new[] { "Name" };
        
        /// <summary>
        /// Returns packages where the search text appears in the default set of properties to search. The default set includes Id, Description and Tags.
        /// </summary>
        public static IQueryable<T> Find<T>(this IQueryable<T> packages, string searchText) where T : Package
        {
            return Find(packages, _packagePropertiesToSearch, searchText);
        }

        /// <summary>
        /// Returns packages where the search text appears in any of the properties to search. 
        /// Note that not all properties can be successfully queried via this method particularly over a OData feed. Verify indepedently if it works for the properties that need to be searched.
        /// </summary>
        public static IQueryable<T> Find<T>(this IQueryable<T> packages, IEnumerable<string> propertiesToSearch, string searchText) where T : Package
        {
            if (propertiesToSearch.IsEmpty())
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "propertiesToSearch");
            }

            if (String.IsNullOrEmpty(searchText))
            {
                return packages;
            }
            return Find(packages, propertiesToSearch, searchText.Split());
        }

        private static IQueryable<T> Find<T>(this IQueryable<T> packages, IEnumerable<string> propertiesToSearch, IEnumerable<string> searchTerms) where T : Package
        {
            if (!searchTerms.Any())
            {
                return packages;
            }

            IEnumerable<string> nonNullTerms = searchTerms.Where(s => s != null);
            if (!nonNullTerms.Any())
            {
                return packages;
            }

            return packages.Where(BuildSearchExpression<T>(propertiesToSearch, nonNullTerms));
        }

        /// <summary>
        /// Constructs an expression to search for individual tokens in a search term in the Id and Description of packages
        /// </summary>
        private static Expression<Func<T, bool>> BuildSearchExpression<T>(IEnumerable<string> propertiesToSearch, IEnumerable<string> searchTerms) where T : Package
        {
            Debug.Assert(searchTerms != null);
            var parameterExpression = Expression.Parameter(typeof(Package));
            // package.Id.ToLower().Contains(term1) || package.Id.ToLower().Contains(term2)  ...
            Expression condition = (from term in searchTerms
                                    from property in propertiesToSearch
                                    select BuildExpressionForTerm(parameterExpression, term, property)).Aggregate(Expression.OrElse);
            return Expression.Lambda<Func<T, bool>>(condition, parameterExpression);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower",
            Justification = "The expression is remoted using Odata which does not support the culture parameter")]
        private static Expression BuildExpressionForTerm(ParameterExpression packageParameterExpression, string term, string propertyName)
        {
            // For tags we want to prepend and append spaces to do an exact match
            if (propertyName.Equals(TagsProperty, StringComparison.OrdinalIgnoreCase))
            {
                term = " " + term + " ";
            }

            MethodInfo stringContains = typeof(String).GetMethod("Contains", new Type[] { typeof(string) });
            MethodInfo stringToLower = typeof(String).GetMethod("ToLower", Type.EmptyTypes);

            // package.Id / package.Description
            var propertyExpression = Expression.Property(packageParameterExpression, propertyName);
            // .ToLower()
            var toLowerExpression = Expression.Call(propertyExpression, stringToLower);

            // Handle potentially null properties
            // package.{propertyName} != null && package.{propertyName}.ToLower().Contains(term.ToLower())
            return Expression.AndAlso(Expression.NotEqual(propertyExpression,
                                                      Expression.Constant(null)),
                                      Expression.Call(toLowerExpression, stringContains, Expression.Constant(term.ToLower())));
        }
    }
}
