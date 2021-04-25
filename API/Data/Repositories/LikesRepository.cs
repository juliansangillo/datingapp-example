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
	public class LikesRepository : ILikesRepository {
        private readonly DataContext context;
        private readonly IMapper mapper;

        public LikesRepository(DataContext context, IMapper mapper) {
            this.context = context;
            this.mapper = mapper;
        }

		public async Task<UserLike> GetUserLike(int sourceUserId, int likedUserId) {
			return await context.Likes.FindAsync(sourceUserId, likedUserId);
		}

		public async Task<PagedList<LikeDto>> GetUserLikes(LikesParams likesParams) {
			IQueryable<AppUser> users = context.Users.AsQueryable();
            IQueryable<UserLike> likes = context.Likes.AsQueryable();

            if(likesParams.Predicate == "liked") {
                likes = likes.Where(like => like.SourceUserId == likesParams.UserId);
                users = likes.Select(like => like.LikedUser);
            }

            if(likesParams.Predicate == "likedBy") {
                likes = likes.Where(like => like.LikedUserId == likesParams.UserId);
                users = likes.Select(like => like.SourceUser);
            }

            IQueryable<LikeDto> likedUsers = users.AsSplitQuery().ProjectTo<LikeDto>(mapper.ConfigurationProvider).OrderBy(l => l.Username);

            return await PagedList<LikeDto>.CreateAsync(likedUsers, likesParams.PageNumber, likesParams.PageSize);
		}

		public async Task<AppUser> GetUserWithLikes(int userId) {
			return await context.Users
                .Include(user => user.Likes)
                .FirstOrDefaultAsync(user => user.Id == userId);
		}
	}
}