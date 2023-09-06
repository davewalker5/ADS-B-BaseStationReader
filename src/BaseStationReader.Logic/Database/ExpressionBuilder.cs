using BaseStationReader.Entities.Expressions;
using BaseStationReader.Entities.Interfaces;
using System.Linq.Expressions;

namespace BaseStationReader.Logic.Database
{
    public class ExpressionBuilder<T> : IExpressionBuilder<T> where T : class
    {
        public IList<TrackerFilter> Filters { get; set; } = new List<TrackerFilter>();

        /// <summary>
        /// Clear the current filters
        /// </summary>
        public void Clear()
        {
            Filters.Clear();
        }

        /// <summary>
        /// Add a clause to the current filters
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="operator"></param>
        /// <param name="value"></param>
        public void Add(string propertyName, TrackerFilterOperator @operator, object? value)
        {
            Filters.Add(new TrackerFilter
            {
                PropertyName = propertyName,
                Operator = @operator,
                Value = value
            });
        }

        /// <summary>
        /// Build a lambda expression from the current filters
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Expression<Func<T, bool>>? Build()
        {
            // If there are no filters, return null
            if (Filters.Count == 0)
            {
                return null;
            }

            // Get an expression representing the object type
            var parameter = Expression.Parameter(typeof(T), "p");

            Expression? body = null;
            foreach (var filter in Filters)
            {
                // Get an expression representing the class member for this filter and a constant representing
                // the value
                var member = Expression.Property(parameter, filter.PropertyName);
                var constant = Expression.Constant(filter.Value);

                // Construct the body of this clause
                Expression expression;
                switch (filter.Operator)
                {
                    case TrackerFilterOperator.Equals:
                        expression = Expression.Equal(member, constant);
                        break;
                    case TrackerFilterOperator.NotEquals:
                        expression = Expression.NotEqual(member, constant);
                        break;
                    case TrackerFilterOperator.GreaterThan:
                        expression = Expression.GreaterThan(member, constant);
                        break;
                    case TrackerFilterOperator.GreaterThanOrEqual:
                        expression = Expression.GreaterThanOrEqual(member, constant);
                        break;
                    case TrackerFilterOperator.LessThan:
                        expression = Expression.LessThan(member, constant);
                        break;
                    case TrackerFilterOperator.LessThanOrEqual:
                        expression = Expression.LessThanOrEqual(member, constant);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                // Add this clause to the body of the overall expression
                body = (body == null) ? expression : Expression.AndAlso(body, expression);
            }

            // Construct and return the lambda expression
            return Expression.Lambda<Func<T, bool>>(body!, parameter);
        }
    }
}
