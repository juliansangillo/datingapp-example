using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers {
	public class AccountController : BaseApiController {
		private readonly DataContext context;
		private readonly ITokenService tokenService;
		private readonly IMapper mapper;

		public AccountController(DataContext context, ITokenService tokenService, IMapper mapper) {
			this.mapper = mapper;
			this.tokenService = tokenService;
			this.context = context;
		}

		[HttpPost("register")]
		public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto) {
			if (await UserExists(registerDto.Username))
				return BadRequest("Username is taken");

			var user = mapper.Map<AppUser>(registerDto);

			using var hmac = new HMACSHA512();

			user.Username = registerDto.Username.ToLower();
			user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
			user.PasswordSalt = hmac.Key;

			context.Users.Add(user);
			await context.SaveChangesAsync();

			return new UserDto {
				Username = user.Username,
				Token = tokenService.CreateToken(user),
				KnownAs = user.KnownAs,
                Gender = user.Gender
			};
		}

		[HttpPost("login")]
		public async Task<ActionResult<UserDto>> Login(LoginDto loginDto) {
			var user = await context.Users.Include(user => user.Photos).SingleOrDefaultAsync(user => user.Username == loginDto.Username);

			if (user == null)
				return Unauthorized("Invalid username");

			using var hmac = new HMACSHA512(user.PasswordSalt);

			var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

			for (int i = 0; i < computedHash.Length; i++) {
				if (computedHash[i] != user.PasswordHash[i])
					return Unauthorized("Invalid password");
			}

			return new UserDto {
				Username = user.Username,
				Token = tokenService.CreateToken(user),
				PhotoUrl = user.Photos.FirstOrDefault(photo => photo.IsMain)?.Url,
				KnownAs = user.KnownAs,
                Gender = user.Gender
			};
		}

		private async Task<bool> UserExists(string username) {

			return await context.Users.AnyAsync(user => user.Username == username.ToLower());
		}
	}
}