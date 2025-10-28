using Bookify.DA.Contracts;
using Bookify.DA.Data;
using Bookify.DA.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Bookify.DA
{
    public static class DataAccessExtensions
    {
        public static IServiceCollection AddDataAccessServices(this IServiceCollection services, string connectionString)
        {
            
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>)); //should be unit of work ... add later



            return services;
        }
    }
}
