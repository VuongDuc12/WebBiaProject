using WebBiaProject.Models;

namespace WebBiaProject.Services.IServices
{
    public interface IBilliardTableService
    {
        Task<List<BilliardTable>> GetAllAsync();
        Task<BilliardTable> GetByIdAsync(int id);
        Task<bool> CreateAsync(BilliardTable entity);
        Task<bool> UpdateAsync(BilliardTable entity);
        Task<bool> DeleteAsync(int id);
    }
}
