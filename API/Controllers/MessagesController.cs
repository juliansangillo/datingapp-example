using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers {
	[Authorize]
	public class MessagesController : BaseApiController {
		private readonly IUserRepository userRepository;
		private readonly IMessageRepository messageRepository;
		private readonly IMapper mapper;

		public MessagesController(IUserRepository userRepository, IMessageRepository messageRepository, IMapper mapper) {
			this.mapper = mapper;
			this.messageRepository = messageRepository;
			this.userRepository = userRepository;
		}

		[HttpPost]
		public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto) {
			var username = User.GetUsername();

			if (username == createMessageDto.RecipientUsername.ToLower())
				return BadRequest("You cannot send messages to yourself");

			var sender = await userRepository.GetUserByUsernameAsync(username);
			var recipient = await userRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

			if (recipient == null)
				return NotFound();

			var message = new Message {
				Sender = sender,
				Recipient = recipient,
				SenderUsername = sender.UserName,
				RecipientUsername = recipient.UserName,
				Content = createMessageDto.Content
			};

			messageRepository.AddMessage(message);

            if(await messageRepository.SaveAllAsync())
                return Ok(mapper.Map<MessageDto>(message));

            return BadRequest("Failed to send message");
		}

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser([FromQuery] MessageParams messageParams) {
            messageParams.Username = User.GetUsername();

            var messages = await messageRepository.GetMessagesForUser(messageParams);

            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);

            return messages;
        }

        [HttpGet("thread/{recipient}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThread(string recipient) {
            var currentUsername = User.GetUsername();

            return Ok(await messageRepository.GetMessageThread(currentUsername, recipient));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id) {
            var username = User.GetUsername();

            var message = await messageRepository.GetMessage(id);

            if(message.Sender.UserName != username && message.Recipient.UserName != username)
                return Unauthorized();

            if(message.Sender.UserName == username)
                message.SenderDeleted = true;

            if(message.Recipient.UserName == username)
                message.RecipientDeleted = true;

            if(message.SenderDeleted && message.RecipientDeleted)
                messageRepository.DeleteMessage(message);

            if(await messageRepository.SaveAllAsync())
                return Ok();

            return BadRequest("Problem deleting the message");
        }
	}
}