using System.Threading.Tasks;
using API.Entities.DB;
using API.Entities.DTOs;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers {
	public class AccountController : ApiController {
		private readonly ITokenService tokenService;
		private readonly IMapper mapper;
		private readonly UserManager<AppUser> userManager;
		private readonly SignInManager<AppUser> signInManager;

		public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService, IMapper mapper) {
			this.signInManager = signInManager;
			this.userManager = userManager;
			this.mapper = mapper;
			this.tokenService = tokenService;
		}

		[HttpPost("register")]
		public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto) {
			if(await UserExists(registerDto.Username))
				return BadRequest("Username is taken");

			AppUser user = mapper.Map<AppUser>(registerDto);

			user.UserName = registerDto.Username.ToLower();

			IdentityResult result = await userManager.CreateAsync(user, registerDto.Password);
            if(!result.Succeeded)
                return BadRequest(result.Errors);

            IdentityResult roleResult = await userManager.AddToRoleAsync(user, "Member");
            if(!roleResult.Succeeded)
                return BadRequest(roleResult.Errors);

            UserDto userDto = mapper.Map<UserDto>(user);
            userDto.Token = await tokenService.CreateToken(user);

			return Ok(userDto);
		}

		[HttpPost("login")]
		public async Task<ActionResult<UserDto>> Login(LoginDto loginDto) {
			AppUser user = await userManager.Users.Include(user => user.Photos).SingleOrDefaultAsync(user => user.UserName == loginDto.Username.ToLower());

			if(user == null)
				return Unauthorized("Invalid username");

			Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if(!result.Succeeded)
                return Unauthorized();
            
            UserDto userDto = mapper.Map<UserDto>(user);
            userDto.Token = await tokenService.CreateToken(user);

			return Ok(userDto);
		}

		private async Task<bool> UserExists(string username) {
			return await userManager.Users.AnyAsync(user => user.UserName == username.ToLower());
		}
	}
}