using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities.DB;
using API.Entities.DTOs;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers {
	public class AdminController : ApiController {
		private readonly UserManager<AppUser> userManager;
		private readonly IMapper mapper;

		public AdminController(UserManager<AppUser> userManager, IMapper mapper) {
			this.mapper = mapper;
			this.userManager = userManager;
		}

		[HttpGet("users-with-roles")]
		[Authorize(Policy = "RequireAdminRole")]
		public async Task<ActionResult> GetUsersWithRoles() {
			IList<UserWithRoles> users = await userManager.Users
				.ProjectTo<UserWithRoles>(mapper.ConfigurationProvider)
                .OrderBy(u => u.Username)
                .ToListAsync();

			return Ok(users);
		}

		[HttpPost("edit-roles/{username}")]
        [Authorize(Policy = "RequireAdminRole")]
		public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles) {
			string[] selectedRoles = roles.Split(",").ToArray();

			AppUser user = await userManager.FindByNameAsync(username);
			if(user == null)
				return NotFound("Could not find user");

			IList<string> userRoles = await userManager.GetRolesAsync(user);

			var result = await userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
			if(!result.Succeeded)
				return BadRequest("Failed to add to roles");

			result = await userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
			if(!result.Succeeded)
				return BadRequest("Failed to remove from roles");

			return Ok(await userManager.GetRolesAsync(user));
		}

		[HttpGet("photos-to-moderate")]
		[Authorize(Policy = "ModeratePhotoRole")]
		public ActionResult GetPhotosForModeration() {
			return Ok("Admins or moderators can see this");
		}
	}
}