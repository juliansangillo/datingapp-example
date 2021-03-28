using System.Threading.Tasks;
using API.Entities;
using API.Entities.DB;
using API.Entities.HTTP;

namespace API.Interfaces {
	public interface IUserRepository {
        void Update(AppUser user);
        Task<PagedList<AppUser>> GetUsersAsync(UserParams userParams);
        Task<AppUser> GetUserByIdAsync(int id);
        Task<AppUser> GetUserByUsernameAsync(string username);
        Task<BasicUser> GetBasicUserByUsernameAsync(string username);
        Task<string> GetUserGender(string username);
	}
}