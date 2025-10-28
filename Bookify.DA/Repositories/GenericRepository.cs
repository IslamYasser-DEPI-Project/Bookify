using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bookify.DA.Contracts;
using Bookify.DA.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookify.DA.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T:class
    {

        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;
        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();

        }
        public async Task<IEnumerable<T>> GetAll()
        {
            return await
                _dbSet.ToListAsync();
        }

        public async Task<T?> GetById(int id)
        {
            return await
                _dbSet.FindAsync(id);

        }

        public async Task Add(T model)
        {
            await
                _dbSet.AddAsync(model);

            await
                _context.SaveChangesAsync();
        }


        public async Task Delete(int id)
        {
            var entity = await GetById(id);
            

            _dbSet.Remove(entity);

            await _context.SaveChangesAsync();

            //NULL EX HANDLE LATER
        }



        public async Task Update(T model)
        {
            _context.Update(model);
            await _context.SaveChangesAsync();

        }

        public IQueryable<T> GetAllQueryable()
        {
            return _dbSet.AsQueryable();
        }
    }
}
