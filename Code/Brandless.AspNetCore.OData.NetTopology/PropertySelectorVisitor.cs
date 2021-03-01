﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Brandless.AspNetCore.OData.NetTopology
{
    internal class PropertySelectorVisitor : ExpressionVisitor
    {
        private List<PropertyInfo> _properties = new List<PropertyInfo>();

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Class is internal, virtual call okay")]
        internal PropertySelectorVisitor(Expression exp)
        {
            Visit(exp);
        }

        public PropertyInfo Property
        {
            get
            {
                return _properties.SingleOrDefault();
            }
        }

        public ICollection<PropertyInfo> Properties
        {
            get
            {
                return _properties;
            }
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node == null)
            {
                throw new Exception("Error.ArgumentNull(\"node\")");
            }

            PropertyInfo pinfo = node.Member as PropertyInfo;

            if (pinfo == null)
            {
                throw new Exception("Error.InvalidOperation(SRResources.MemberExpressionsMustBeProperties, TypeHelper.GetReflectedType(node.Member).FullName, node.Member.Name)");
            }

            if (node.Expression.NodeType != ExpressionType.Parameter)
            {
                throw new Exception("Error.InvalidOperation(SRResources.MemberExpressionsMustBeBoundToLambdaParameter)");
            }

            _properties.Add(pinfo);
            return node;
        }

        public static PropertyInfo GetSelectedProperty(Expression exp)
        {
            return new PropertySelectorVisitor(exp).Property;
        }

        public static ICollection<PropertyInfo> GetSelectedProperties(Expression exp)
        {
            return new PropertySelectorVisitor(exp).Properties;
        }

        public override Expression Visit(Expression exp)
        {
            if (exp == null)
            {
                return exp;
            }

            switch (exp.NodeType)
            {
                case ExpressionType.New:
                case ExpressionType.MemberAccess:
                case ExpressionType.Lambda:
                    return base.Visit(exp);
                default:
                    throw new Exception("Error.NotSupported(SRResources.UnsupportedExpressionNodeType)");
            }
        }

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            if (lambda == null)
            {
                throw new Exception("Error.ArgumentNull(\"lambda\")");
            }

            if (lambda.Parameters.Count != 1)
            {
                throw new Exception("Error.InvalidOperation(SRResources.LambdaExpressionMustHaveExactlyOneParameter)");
            }

            Expression body = Visit(lambda.Body);

            if (body != lambda.Body)
            {
                return Expression.Lambda(lambda.Type, body, lambda.Parameters);
            }
            return lambda;
        }
    }
}
