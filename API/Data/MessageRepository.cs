using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data {
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
				.AsQueryable();

			query = messageParams.Container switch {
				"Inbox" => query.Where(u => u.Recipient.UserName == messageParams.Username
                    && !u.RecipientDeleted),
				"Outbox" => query.Where(u => u.Sender.UserName == messageParams.Username
                    && !u.SenderDeleted),
				_ => query.Where(u => u.Recipient.UserName == messageParams.Username
                    && !u.RecipientDeleted && u.DateRead == null)
			};

            var messages = query.ProjectTo<MessageDto>(mapper.ConfigurationProvider);

            return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
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
                .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null && m.Recipient.UserName == currentUsername).ToList();
            if(unreadMessages.Any()) {
                foreach(var message in unreadMessages) {
                    message.DateRead = DateTime.UtcNow;
                }

                await context.SaveChangesAsync();
            }

            return mapper.Map<IEnumerable<MessageDto>>(messages);
		}

		public void RemoveConnection(Connection connection) {
			context.Connections.Remove(connection);
		}

		public async Task<bool> SaveAllAsync() {
			return await context.SaveChangesAsync() > 0;
		}
	}
}