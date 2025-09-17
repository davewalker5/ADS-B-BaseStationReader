using BaseStationReader.Entities.Expressions;
using System.Linq.Expressions;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IExpressionBuilder<T> where T : class
    {
        IList<TrackerFilter> Filters { get; set; }

        void Add(string propertyName, TrackerFilterOperator @operator, object value);
        Expression<Func<T, bool>> Build();
        void Clear();
    }
}