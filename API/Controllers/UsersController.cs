using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Extensions;
using API.Entities.HTTP;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Entities.DTOs;
using API.Entities.DB;
using API.Entities;
using CloudinaryDotNet.Actions;

namespace API.Controllers {
	[Authorize]
	public class UsersController : ApiController {
		private readonly IMapper mapper;
		private readonly IPhotoService photoService;
		private readonly IUnitOfWork unitOfWork;

		public UsersController(IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService) {
			this.unitOfWork = unitOfWork;
			this.photoService = photoService;
			this.mapper = mapper;
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery] UserParams userParams) {
			string gender = await unitOfWork.UserRepository.GetUserGender(User.GetUsername());
			userParams.CurrentUsername = User.GetUsername();
			if(string.IsNullOrEmpty(userParams.Gender))
				userParams.Gender = gender == "male" ? "female" : "male";

			PagedList<MemberDto> users = await unitOfWork.UserRepository.GetUsersAsync(userParams);

			Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

			return Ok(users);
		}

		[HttpGet("{username}", Name = "GetUser")]
		public async Task<ActionResult<MemberDto>> GetUser(string username) {
			return Ok(await unitOfWork.UserRepository.GetMemberByUsernameAsync(username));
		}

		[HttpPut]
		public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto) {
			AppUser user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

			mapper.Map(memberUpdateDto, user);

			unitOfWork.UserRepository.Update(user);

			if(await unitOfWork.Complete())
				return NoContent();

			return BadRequest("Failed to update user");
		}

		[HttpPost("add-photo")]
		public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file) {
			AppUser user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

			ImageUploadResult result = await photoService.AddPhotoAsync(file);

			if(result.Error != null)
				return BadRequest(result.Error.Message);

			Photo photo = new Photo {
				Url = result.SecureUrl.AbsoluteUri,
				PublicId = result.PublicId
			};

			if(user.Photos.Count == 0) {
				photo.IsMain = true;
			}

			user.Photos.Add(photo);

			if (await unitOfWork.Complete())
				return CreatedAtRoute("GetUser", new { username = user.UserName }, mapper.Map<PhotoDto>(photo));

			return BadRequest("Problem adding photo");
		}

		[HttpPut("set-main-photo/{photoId}")]
		public async Task<ActionResult> SetMainPhoto(int photoId) {
			AppUser user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());

			Photo photo = user.Photos.FirstOrDefault(photo => photo.Id == photoId);

			if(photo.IsMain)
				return BadRequest("This is already your main photo");

			Photo currentMain = user.Photos.FirstOrDefault(photo => photo.IsMain);
			if(currentMain != null)
				currentMain.IsMain = false;
			photo.IsMain = true;

			if(await unitOfWork.Complete())
				return NoContent();

			return BadRequest("Failed to set main photo");
		}

		[HttpDelete("delete-photo/{photoId}")]
		public async Task<ActionResult> DeletePhoto(int photoId) {
			AppUser user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());
			Photo photo = user.Photos.FirstOrDefault(photo => photo.Id == photoId);

			if(photo == null)
				return NotFound();

			if(photo.IsMain)
				return BadRequest("You cannot delete your main photo");

			if(photo.PublicId != null) {
				DeletionResult result = await photoService.DeletePhotoAsync(photo.PublicId);
				if(result.Error != null)
					return BadRequest(result.Error.Message);
			}

			user.Photos.Remove(photo);

			if(await unitOfWork.Complete())
				return Ok();

			return BadRequest("Failed to delete the photo");
		}
	}
}