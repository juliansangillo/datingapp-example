using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities.DB;
using API.Entities.DTOs;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories {
	public class PhotoRepository : IPhotoRepository {
		private readonly DataContext context;
		private readonly IMapper mapper;

		public PhotoRepository(DataContext context, IMapper mapper) {
			this.mapper = mapper;
			this.context = context;
		}

		public async Task<Photo> GetPhotoById(int id) {
			return await context.Photos.FindAsync(id);
		}

		public async Task<ICollection<PhotoForApprovalDto>> GetUnapprovedPhotos() {
			return await context.Photos
				.Where(p => !p.IsApproved)
                .ProjectTo<PhotoForApprovalDto>(mapper.ConfigurationProvider)
				.OrderBy(p => p.Id)
				.ToListAsync();
		}

		public void RemovePhoto(Photo photo) {
			context.Photos.Remove(photo);
		}
	}
}