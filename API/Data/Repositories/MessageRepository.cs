using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Entities.DB;
using API.Entities.DTOs;
using API.Entities.HTTP;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories {
	public class MessageRepository : IMessageRepository {
		private readonly DataContext context;
		private readonly IMapper mapper;

		public MessageRepository(DataContext context, IMapper mapper) {
			this.mapper = mapper;
			this.context = context;
		}

		public void AddGroup(Group group) {
			context.Groups.Add(group);
		}

		public void AddMessage(Message message) {
			context.Messages.Add(message);
		}

		public void DeleteMessage(Message message) {
			context.Messages.Remove(message);
		}

		public async Task<Connection> GetConnection(string connectionId) {
			return await context.Connections.FindAsync(connectionId);
		}

		public async Task<Group> GetGroupForConnection(string connectionId) {
			return await context.Groups
                .Include(c => c.Connections)
                .Where(c => c.Connections.Any(c => c.ConnectionId == connectionId))
                .FirstOrDefaultAsync();
		}

		public async Task<Message> GetMessage(int id) {
			return await context.Messages
                .Include(u => u.Sender)
                .Include(u => u.Recipient)
                .SingleOrDefaultAsync(x => x.Id == id);
		}

		public async Task<Group> GetMessageGroup(string groupName) {
			return await context.Groups
                .Include(group => group.Connections)
                .FirstOrDefaultAsync(group => group.Name == groupName);
		}

		public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams) {
			var query = context.Messages
				.OrderByDescending(m => m.MessageSent)
                .Include(u => u.Sender.Photos)
                .Include(u => u.Recipient.Photos)
                .ProjectTo<MessageDto>(mapper.ConfigurationProvider)
				.AsQueryable();

			query = messageParams.Container switch {
				"Inbox" => query.Where(u => u.RecipientUsername == messageParams.Username
                    && !u.RecipientDeleted),
				"Outbox" => query.Where(u => u.SenderUsername == messageParams.Username
                    && !u.SenderDeleted),
				_ => query.Where(u => u.RecipientUsername == messageParams.Username
                    && !u.RecipientDeleted && u.DateRead == null)
			};

            return await PagedList<MessageDto>.CreateAsync(query, messageParams.PageNumber, messageParams.PageSize);
		}

		public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername) {
			var messages = await context.Messages
                .Include(u => u.Sender).ThenInclude(p => p.Photos)
                .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                .Where(m => m.Recipient.UserName == currentUsername && !m.RecipientDeleted
                    && m.Sender.UserName == recipientUsername
                    || m.Recipient.UserName == recipientUsername
                    && m.Sender.UserName == currentUsername && !m.SenderDeleted
                )
                .OrderBy(m => m.MessageSent)
                .ProjectTo<MessageDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null && m.RecipientUsername == currentUsername).ToList();
            if(unreadMessages.Any()) {
                foreach(var message in unreadMessages) {
                    message.DateRead = DateTime.UtcNow;
                }
            }

            return messages;
		}

		public void RemoveConnection(Connection connection) {
			context.Connections.Remove(connection);
		}
	}
}