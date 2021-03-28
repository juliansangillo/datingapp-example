using System.Collections.Generic;
using System.Threading.Tasks;
using API.Extensions;
using API.Entities.HTTP;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Entities.DB;
using API.Entities.DTOs;
using API.Entities;

namespace API.Controllers {
	[Authorize]
	public class LikesController : BaseApiController {
		private readonly IUnitOfWork unitOfWork;

		public LikesController(IUnitOfWork unitOfWork) {
			this.unitOfWork = unitOfWork;
		}

		[HttpPost("{username}")]
		public async Task<ActionResult> AddLike(string username) {
			var sourceUserId = User.GetUserId();
			var likedUser = await unitOfWork.UserRepository.GetBasicUserByUsernameAsync(username);
			var sourceUser = await unitOfWork.LikesRepository.GetUserWithLikes(sourceUserId);

			if (likedUser == null)
				return NotFound();

			if (sourceUser.UserName == username)
				return BadRequest("You cannot like yourself");

			var userLike = await unitOfWork.LikesRepository.GetUserLike(sourceUserId, likedUser.Id);

			if (userLike != null)
				return BadRequest("You already like this user");

			userLike = new UserLike {
				SourceUserId = sourceUserId,
				LikedUserId = likedUser.Id
			};

			sourceUser.Likes.Add(userLike);

			if(await unitOfWork.Complete())
				return Ok();

			return BadRequest("Failed to like user");
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<LikeDto>>> GetUserLikes([FromQuery] LikesParams likesParams) {
			likesParams.UserId = User.GetUserId();
			var users = await unitOfWork.LikesRepository.GetUserLikes(likesParams);

			Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

			return Ok(users);
		}
	}
}