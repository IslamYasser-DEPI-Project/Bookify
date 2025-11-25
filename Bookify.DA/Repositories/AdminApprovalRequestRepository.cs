using Bookify.DA.Contracts.RepositoryContracts;
using Bookify.DA.Data;
using Bookify.DA.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Bookify.DA.Repositories
{
    public class AdminApprovalRequestRepository : GenericRepository<AdminApprovalRequest>, IAdminApprovalRequest
    {
        public AdminApprovalRequestRepository(AppDbContext context) : base(context)
        {
        }
    }
  
}
