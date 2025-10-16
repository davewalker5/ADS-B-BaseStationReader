using BaseStationReader.Entities.Api;
using System.Linq.Expressions;

namespace BaseStationReader.Interfaces.Database
{
    public interface IModelManager
    {
        Task<Model> GetAsync(string iata, string icao, string name);
        Task<Model> GetAsync(Expression<Func<Model, bool>> predicate);
        Task<List<Model>> ListAsync(Expression<Func<Model, bool>> predicate);
        Task<Model> AddAsync(string iata, string icao, string name, int manufacturerId);
    }
}