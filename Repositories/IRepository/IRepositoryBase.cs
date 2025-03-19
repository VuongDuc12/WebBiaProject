using Microsoft.AspNetCore.Identity;
using WebBiaProject.Data;
using WebBiaProject.Models;

namespace WebBiaProject.Repositories.IRepository
{
    public interface IRepositoryBase<T> where T : class
    {
        Task<List<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        Task CreateAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
    

    }
}
