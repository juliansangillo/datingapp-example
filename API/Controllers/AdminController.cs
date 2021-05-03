using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities.DB;
using API.Entities.DTOs;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers {
	public class AdminController : ApiController {
		private readonly UserManager<AppUser> userManager;
		private readonly IUnitOfWork unitOfWork;
		private readonly IPhotoService photoService;
		private readonly IMapper mapper;

		public AdminController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork, IPhotoService photoService, IMapper mapper) {
			this.mapper = mapper;
            this.photoService = photoService;
			this.unitOfWork = unitOfWork;
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
			if (user == null)
				return NotFound("Could not find user");

			IList<string> userRoles = await userManager.GetRolesAsync(user);

			var result = await userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
			if (!result.Succeeded)
				return BadRequest("Failed to add to roles");

			result = await userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
			if (!result.Succeeded)
				return BadRequest("Failed to remove from roles");

			return Ok(await userManager.GetRolesAsync(user));
		}

		[HttpGet("photos-to-moderate")]
		[Authorize(Policy = "ModeratePhotoRole")]
		public async Task<ActionResult> GetPhotosForModeration() {
			return Ok(await unitOfWork.PhotoRepository.GetUnapprovedPhotos());
		}

		[HttpPut("photo-for-approval/{id}")]
		[Authorize(Policy = "ModeratePhotoRole")]
		public async Task<ActionResult> ApprovePhoto(int id) {
			Photo photo = await unitOfWork.PhotoRepository.GetPhotoById(id);
			if (photo == null)
				return NotFound("Could not find photo");

			MemberDto user = await unitOfWork.UserRepository.GetMemberByPhotoIdAsync(id);
			if (user == null)
				return BadRequest("No user exists with photo");

			if (user.Photos.SingleOrDefault(p => p.IsMain) == null)
				photo.IsMain = true;

			photo.IsApproved = true;

			if (await unitOfWork.Complete())
				return NoContent();

			return BadRequest("Failed to approve photo");
		}

		[HttpDelete("photo-for-approval/{id}")]
		[Authorize(Policy = "ModeratePhotoRole")]
		public async Task<ActionResult> RejectPhoto(int id) {
			Photo photo = await unitOfWork.PhotoRepository.GetPhotoById(id);
			if (photo == null)
				return NotFound("Could not find photo");

            unitOfWork.PhotoRepository.RemovePhoto(photo);

            DeletionResult result = await photoService.DeletePhotoAsync(photo.PublicId);
            if (result.Error != null)
                return BadRequest(result.Error.Message);

			if (await unitOfWork.Complete())
				return NoContent();

			return BadRequest("Failed to reject photo");
		}
	}
}