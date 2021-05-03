using System.Threading.Tasks;

namespace API.Interfaces {
	public interface IUnitOfWork {
        IUserRepository UserRepository { get; }
        IPhotoRepository PhotoRepository { get; }
        IMessageRepository MessageRepository { get; }
        ILikesRepository LikesRepository { get; }
        
        Task<bool> Complete();
        bool HasChanges();
	}
}