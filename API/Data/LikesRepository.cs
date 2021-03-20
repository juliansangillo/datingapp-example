using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data {
	public class LikesRepository : ILikesRepository {
        private readonly DataContext context;
        private readonly IMapper mapper;
        private readonly IConfigurationProvider configuration;

        public LikesRepository(DataContext context, IMapper mapper, IConfigurationProvider configuration) {
            this.context = context;
            this.mapper = mapper;
            this.configuration = configuration;
        }

		public async Task<UserLike> GetUserLike(int sourceUserId, int likedUserId) {
			return await context.Likes.FindAsync(sourceUserId, likedUserId);
		}

		public async Task<PagedList<LikeDto>> GetUserLikes(LikesParams likesParams) {
			var users = context.Users.OrderBy(u => u.Username).AsQueryable();
            var likes = context.Likes.AsQueryable();

            if(likesParams.Predicate == "liked") {
                likes = likes.Where(like => like.SourceUserId == likesParams.UserId);
                users = likes.Include(like => like.LikedUser.Photos).Select(like => like.LikedUser);
            }

            if(likesParams.Predicate == "likedBy") {
                likes = likes.Where(like => like.LikedUserId == likesParams.UserId);
                users = likes.Include(like => like.SourceUser.Photos).Select(like => like.SourceUser);
            }

            var likedUsers = users.Select(user => mapper.Map<LikeDto>(user));

            return await PagedList<LikeDto>.CreateAsync(likedUsers, likesParams.PageNumber, likesParams.PageSize);
		}

		public async Task<AppUser> GetUserWithLikes(int userId) {
			return await context.Users
                .Include(x => x.Likes)
                .FirstOrDefaultAsync(x => x.Id == userId);
		}
	}
}