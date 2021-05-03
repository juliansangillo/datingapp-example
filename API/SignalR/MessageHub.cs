using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities.DB;
using API.Entities.DTOs;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR {
	public class MessageHub : Hub {
		private readonly IMapper mapper;
		private readonly IHubContext<PresenceHub> presenceHub;
		private readonly PresenceTracker tracker;
		private readonly IUnitOfWork unitOfWork;

		public MessageHub(IUnitOfWork unitOfWork, IMapper mapper, IHubContext<PresenceHub> presenceHub,
				PresenceTracker tracker) {
			this.unitOfWork = unitOfWork;
			this.tracker = tracker;
			this.presenceHub = presenceHub;
			this.mapper = mapper;
		}

		public override async Task OnConnectedAsync() {
			HttpContext httpContext = Context.GetHttpContext();
			string otherUser = httpContext.Request.Query["user"].ToString();
			string groupName = GetGroupName(Context.User.GetUsername(), otherUser);

			await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
			Group group = await AddToGroup(groupName);
			await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

			IEnumerable<MessageDto> messages = await unitOfWork.MessageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);

            if(unitOfWork.HasChanges())
                await unitOfWork.Complete();

			await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
		}

		public override async Task OnDisconnectedAsync(Exception exception) {
			Group group = await RemoveFromMessageGroup();
			await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
			await base.OnDisconnectedAsync(exception);
		}

		public async Task SendMessage(CreateMessageDto createMessageDto) {
			string username = Context.User.GetUsername();

			if(username == createMessageDto.RecipientUsername.ToLower())
				throw new HubException("You cannot send messages to yourself");

			AppUser sender = await unitOfWork.UserRepository.GetUserByUsernameAsync(username);
			AppUser recipient = await unitOfWork.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

			if(recipient == null)
				throw new HubException("Not found user");

			Message message = new Message {
				Sender = sender,
				Recipient = recipient,
				SenderUsername = sender.UserName,
				RecipientUsername = recipient.UserName,
				Content = createMessageDto.Content
			};

			string groupName = GetGroupName(sender.UserName, recipient.UserName);
			Group group = await unitOfWork.MessageRepository.GetMessageGroup(groupName);
			if(group.Connections.Any(conn => conn.Username == recipient.UserName)) {
				message.DateRead = DateTime.UtcNow;
			} else {
				List<string> connections = await tracker.GetConnectionsForUser(recipient.UserName);
				if(connections != null) {
					await presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", new {
						username = sender.UserName,
						knownAs = sender.KnownAs
					});
				}
			}

			unitOfWork.MessageRepository.AddMessage(message);

			if(await unitOfWork.Complete())
				await Clients.Group(groupName).SendAsync("NewMessage", mapper.Map<MessageDto>(message));
		}

		private async Task<Group> AddToGroup(string groupName) {
			Group group = await unitOfWork.MessageRepository.GetMessageGroup(groupName);
			Connection connection = new Connection(Context.ConnectionId, Context.User.GetUsername());

			if(group == null) {
				group = new Group(groupName);
				unitOfWork.MessageRepository.AddGroup(group);
			}

			group.Connections.Add(connection);

			if(await unitOfWork.Complete())
				return group;

			throw new HubException("Failed to join group");
		}

		private async Task<Group> RemoveFromMessageGroup() {
			Group group = await unitOfWork.MessageRepository.GetGroupForConnection(Context.ConnectionId);
			Connection connection = group.Connections.FirstOrDefault(c => c.ConnectionId == Context.ConnectionId);
			unitOfWork.MessageRepository.RemoveConnection(connection);

			if (await unitOfWork.Complete())
				return group;

			throw new HubException("Failed to remove from group");
		}

		private string GetGroupName(string caller, string other) {
			bool stringCompare = string.CompareOrdinal(caller, other) < 0;

			return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
		}
	}
}