using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DentissaDbContext _context;

        public AuthRepository(DentissaDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
            => await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<Usuario?> GetByIdAsync(int id)
            => await _context.Usuarios.FindAsync(id);

        public async Task<Usuario?> GetByRefreshTokenAsync(string refreshToken)
            => await _context.Usuarios.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        public async Task<Usuario> CreateAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task UpdateAsync(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
            => await _context.Usuarios.AnyAsync(u => u.Email == email);
        
        public async Task<List<Usuario>> GetAllAsync()
             => await _context.Usuarios.ToListAsync();
 
        public async Task DeleteAsync(Usuario usuario)
         {
            _context.Usuarios.Remove(usuario);
             await _context.SaveChangesAsync();
        }
 
    }
}