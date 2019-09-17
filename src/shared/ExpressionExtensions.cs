using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace shared
{
    public static class ExpressionExtensions
    {
        public static string GetMemberName<T>(this Expression<Func<T, object>> expression)
        {
            string GetMemberName(Expression value)
            {
                switch (value)
                {
                    case UnaryExpression unaryExpression:
                        return GetMemberName(unaryExpression.Operand as MemberExpression);
                    case MemberExpression memberExpression:
                        return memberExpression.Member.Name;
                }
                throw new ArgumentException("Cannot get member name from expression.");
            }

            return GetMemberName(expression.Body);
        }
    }
}
