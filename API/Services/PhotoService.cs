using System.IO;
using System.Threading.Tasks;
using API.Interfaces;
using API.Settings;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace API.Services {
	public class PhotoService : IPhotoService {
        private readonly Cloudinary cloudinary;

		public PhotoService(IOptions<CloudinarySettings> config) {
            Account account = new Account (
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );

            this.cloudinary = new Cloudinary(account);
		}

		public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file) {
			ImageUploadResult uploadResult = new ImageUploadResult();

            if(file.Length > 0) {
                using Stream stream = file.OpenReadStream();
                ImageUploadParams uploadParams = new ImageUploadParams {
                    File = new FileDescription(file.FileName, stream),
                    Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face")
                };
                uploadResult = await cloudinary.UploadAsync(uploadParams);
            }

            return uploadResult;
		}

		public async Task<DeletionResult> DeletePhotoAsync(string publicId) {
			DeletionParams deleteParams = new DeletionParams(publicId);

            DeletionResult result = await cloudinary.DestroyAsync(deleteParams);

            return result;
		}
	}
}