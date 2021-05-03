using System.Threading.Tasks;
using API.Entities.DTOs;
using API.Entities.DB;
using API.Entities.HTTP;
using API.Entities;

namespace API.Interfaces {
	public interface ILikesRepository {
        Task<UserLike> GetUserLike(int sourceUserId, int LikedUserId);
        Task<AppUser> GetUserWithLikes(int userId);
        Task<PagedList<LikeDto>> GetUserLikes(LikesParams likesParams);
	}
}