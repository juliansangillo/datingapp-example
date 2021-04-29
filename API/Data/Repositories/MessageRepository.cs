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
			IQueryable<MessageDto> query = context.Messages
                .ProjectTo<MessageDto>(mapper.ConfigurationProvider)
                .OrderByDescending(m => m.MessageSent)
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
			IList<Message> messages = await context.Messages
                .Include(m => m.Sender).ThenInclude(u => u.Photos)
                .Include(m => m.Recipient).ThenInclude(u => u.Photos)
                .Where(
                    m => m.Recipient.UserName == currentUsername && m.Sender.UserName == recipientUsername && !m.RecipientDeleted ||
                    m.Recipient.UserName == recipientUsername && m.Sender.UserName == currentUsername && !m.SenderDeleted
                )
                .OrderBy(m => m.MessageSent)
                .ToListAsync();

            IList<Message> unreadMessages = messages.Where(m => m.DateRead == null && m.RecipientUsername == currentUsername).ToList();
            if(unreadMessages.Any())
                foreach(Message message in unreadMessages)
                    message.DateRead = DateTime.UtcNow;

            return mapper.Map<IEnumerable<MessageDto>>(messages);
		}

		public void RemoveConnection(Connection connection) {
			context.Connections.Remove(connection);
		}
	}
}