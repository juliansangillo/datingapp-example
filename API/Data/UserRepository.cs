using System;
using System.Collections.Generic;
using System.Linq;
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
				.SingleOrDefaultAsync(user => user.UserName == username);
		}

		public async Task<PagedList<AppUser>> GetUsersAsync(UserParams userParams) {
			
            var query = context.Users
				.Include(user => user.Photos)
                .AsNoTracking()
                .AsQueryable();

            query = query.Where(u => u.UserName != userParams.CurrentUsername);
            query = query.Where(u => u.Gender == userParams.Gender);

            var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
            var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

            query = query.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);

            query = userParams.OrderBy switch {
                "created" => query.OrderByDescending(u => u.Created),
                _ => query.OrderByDescending(u => u.LastActive)
            };

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