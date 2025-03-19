using NuGet.Protocol.Core.Types;
using WebBiaProject.Models;
using WebBiaProject.Repositories.IRepository;
using WebBiaProject.Services.IServices;

namespace WebBiaProject.Services.Services
{
    public class BilliardTableService: IBilliardTableService
    {
        private readonly IBilliardTableRepository _repository;

        public BilliardTableService(IBilliardTableRepository repository)
        {
            _repository = repository;
        }
        public async Task<List<BilliardTable>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<BilliardTable> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> CreateAsync(BilliardTable entity)
        {
            try
            {
                await _repository.CreateAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateAsync(BilliardTable entity)
        {
            try
            {
                await _repository.UpdateAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
           
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                await _repository.DeleteAsync(id);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
           
        }
    }
}
