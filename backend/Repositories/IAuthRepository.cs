using backend.Entities;

namespace backend.Repositories
{
    public interface IAuthRepository
    {
        Task<Usuario?> GetByEmailAsync(string email);
        Task<Usuario?> GetByIdAsync(int id);
        Task<Usuario?> GetByRefreshTokenAsync(string refreshToken);
        Task<Usuario> CreateAsync(Usuario usuario);
        Task UpdateAsync(Usuario usuario);
        Task<bool> EmailExistsAsync(string email);
        Task<List<Usuario>> GetAllAsync();
    }
}