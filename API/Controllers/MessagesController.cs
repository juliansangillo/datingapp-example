using System.Collections.Generic;
using System.Threading.Tasks;
using API.Extensions;
using API.Entities.HTTP;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Entities.DTOs;
using API.Entities.DB;
using API.Entities;

namespace API.Controllers {
	[Authorize]
	public class MessagesController : ApiController {
		private readonly IMapper mapper;
		private readonly IUnitOfWork unitOfWork;

		public MessagesController(IUnitOfWork unitOfWork, IMapper mapper) {
			this.unitOfWork = unitOfWork;
			this.mapper = mapper;
		}

		[HttpPost]
		public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto) {
			string username = User.GetUsername();

			if(username == createMessageDto.RecipientUsername.ToLower())
				return BadRequest("You cannot send messages to yourself");

			AppUser sender = await unitOfWork.UserRepository.GetUserByUsernameAsync(username);
			AppUser recipient = await unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

			if(recipient == null)
				return NotFound();

			Message message = new Message {
				Sender = sender,
				Recipient = recipient,
				SenderUsername = sender.UserName,
				RecipientUsername = recipient.UserName,
				Content = createMessageDto.Content
			};

			unitOfWork.MessageRepository.AddMessage(message);

			if(await unitOfWork.Complete())
				return Ok(mapper.Map<MessageDto>(message));

			return BadRequest("Failed to send message");
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser([FromQuery] MessageParams messageParams) {
			messageParams.Username = User.GetUsername();

			PagedList<MessageDto> messages = await unitOfWork.MessageRepository.GetMessagesForUser(messageParams);

			Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);

			return messages;
		}

		[HttpDelete("{id}")]
		public async Task<ActionResult> DeleteMessage(int id) {
			string username = User.GetUsername();

			Message message = await unitOfWork.MessageRepository.GetMessage(id);

			if(message.Sender.UserName != username && message.Recipient.UserName != username)
				return Unauthorized();

			if(message.Sender.UserName == username)
				message.SenderDeleted = true;

			if(message.Recipient.UserName == username)
				message.RecipientDeleted = true;

			if(message.SenderDeleted && message.RecipientDeleted)
				unitOfWork.MessageRepository.DeleteMessage(message);

			if(await unitOfWork.Complete())
				return Ok();

			return BadRequest("Problem deleting the message");
		}
	}
}