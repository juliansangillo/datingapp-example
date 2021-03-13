using System.Collections.Generic;
using System.Threading.Tasks;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data {
	public class UserRepository : IUserRepository {
		private readonly DataContext context;

		public UserRepository(DataContext context) {
			this.context = context;
		}

		public async Task<AppUser> GetUserByIdAsync(int id) {
			
            return await context.Users.FindAsync(id);
		}

		public async Task<AppUser> GetUserByUsernameAsync(string username) {
			
            return await context.Users
				.Include(user => user.Photos)
				.SingleOrDefaultAsync(user => user.Username == username);
		}

		public async Task<PagedList<AppUser>> GetUsersAsync(UserParams userParams) {
			
            var query = context.Users
				.Include(user => user.Photos)
                .AsNoTracking();

            return await PagedList<AppUser>.CreateAsync(query, userParams.PageNumber, userParams.PageSize);
		}

		public async Task<bool> SaveAllAsync() {
			
            return await context.SaveChangesAsync() > 0;
		}

		public void Update(AppUser user) {
			context.Entry(user).State = EntityState.Modified;
		}
	}
}