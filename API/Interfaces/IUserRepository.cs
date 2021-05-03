using System.Threading.Tasks;
using API.Entities;
using API.Entities.DB;
using API.Entities.DTOs;
using API.Entities.HTTP;

namespace API.Interfaces {
	public interface IUserRepository {
        void Update(AppUser user);
        Task<PagedList<MemberDto>> GetUsersAsync(UserParams userParams);
        Task<AppUser> GetUserByIdAsync(int id);
        Task<AppUser> GetUserByUsernameAsync(string username);
        Task<MemberDto> GetMemberByUsernameAsync(string username, string currentUsername);
        Task<MemberDto> GetMemberByPhotoIdAsync(int photoId);
        Task<BasicUser> GetBasicUserByUsernameAsync(string username);
        Task<string> GetUserGender(string username);
	}
}