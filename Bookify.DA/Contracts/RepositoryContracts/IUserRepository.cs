using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bookify.DA.Entities;

namespace Bookify.DA.Contracts.RepositoryContracts
{
    public interface IUserRepository : IGenericRepository<User>
    {
    }
}
