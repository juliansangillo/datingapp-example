using System.Threading.Tasks;
using API.Entities.DB;

namespace API.Interfaces {
	public interface ITokenService {
        Task<string> CreateToken(AppUser user);
    }
}