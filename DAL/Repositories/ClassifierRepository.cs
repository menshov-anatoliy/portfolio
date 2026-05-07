using Molcom.DAL.Entities.Tables.Terminals.Classifier;
using Molcom.DAL.SqlServer.Repositories.Interfaces;

namespace Molcom.DAL.SqlServer.Repositories;

public class ClassifierRepository : Repository<ClassifierDb>, IClassifierRepository
{
}

public interface IClassifierRepository : IReadRepository<ClassifierDb>
{
}