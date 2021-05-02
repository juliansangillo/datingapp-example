using System;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Entities.DB;
using API.Entities.DTOs;
using API.Entities.HTTP;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories {
	public class UserRepository : IUserRepository {
		private readonly DataContext context;
		private readonly IMapper mapper;

		public UserRepository(DataContext context, IMapper mapper) {
			this.mapper = mapper;
			this.context = context;
		}

		public async Task<AppUser> GetUserByIdAsync(int id) {
			return await context.Users.FindAsync(id);
		}

		public async Task<AppUser> GetUserByUsernameAsync(string username) {
			return await context.Users
                .IgnoreQueryFilters()
				.Include(user => user.Photos)
				.SingleOrDefaultAsync(user => user.UserName == username);
		}

        public async Task<MemberDto> GetMemberByUsernameAsync(string username) {
            return await context.Users
                .IgnoreQueryFilters()
                .Where(user => user.UserName == username)
                .ProjectTo<MemberDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        public async Task<BasicUser> GetBasicUserByUsernameAsync(string username) {
            return await context.Users
                .Where(user => user.UserName == username)
                .ProjectTo<BasicUser>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

		public async Task<string> GetUserGender(string username) {
			return await context.Users.Where(x => x.UserName == username).Select(x => x.Gender).FirstOrDefaultAsync();
		}

		public async Task<PagedList<MemberDto>> GetUsersAsync(UserParams userParams) {
			IQueryable<AppUser> query = context.Users
				.AsNoTracking()
				.AsQueryable();

			query = query.Where(u => u.UserName != userParams.CurrentUsername);
			query = query.Where(u => u.Gender == userParams.Gender);

			DateTime minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
			DateTime maxDob = DateTime.Today.AddYears(-userParams.MinAge);

			query = query.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);

			query = userParams.OrderBy switch {
				"created" => query.OrderByDescending(u => u.Created),
				_ => query.OrderByDescending(u => u.LastActive)
			};

			return await PagedList<MemberDto>.CreateAsync(query.ProjectTo<MemberDto>(mapper.ConfigurationProvider), userParams.PageNumber, userParams.PageSize);
		}

		public void Update(AppUser user) {
			context.Entry(user).State = EntityState.Modified;
		}
	}
}