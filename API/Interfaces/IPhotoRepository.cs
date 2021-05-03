using System.Collections.Generic;
using System.Threading.Tasks;
using API.Entities.DB;
using API.Entities.DTOs;

namespace API.Interfaces {
	public interface IPhotoRepository {
        Task<ICollection<PhotoForApprovalDto>> GetUnapprovedPhotos();
        Task<Photo> GetPhotoById(int id);
        void RemovePhoto(Photo photo);
	}
}