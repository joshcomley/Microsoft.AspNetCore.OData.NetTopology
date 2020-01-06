using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.OData.NetTopology.Visitors
{
    /// <summary>
    /// The base class for all expression binders.
    /// </summary>
    public class ExpressionHelper
    {
        internal static IEnumerable<Expression> ExtractValueFromNullableArguments(IEnumerable<Expression> arguments)
        {
            return arguments.Select(ExtractValueFromNullableExpression);
        }

        internal static Expression ExtractValueFromNullableExpression(Expression source)
        {
            return Nullable.GetUnderlyingType(source.Type) != null ? Expression.Property(source, "Value") : source;
        }
    }
}
