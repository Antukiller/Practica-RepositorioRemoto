using Practicas_RepositorioRemoto.Models;
using Practicas_RepositorioRemoto.Repository.Common;

namespace Practicas_RepositorioRemoto.Repository;

public interface IUserRepository : ICrudRepository<int, User> {
    
}