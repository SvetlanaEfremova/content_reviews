using CloudinaryDotNet.Actions;
using CloudinaryDotNet;

namespace course_project.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );
            cloudinary = new Cloudinary(account);
        }

        public string UploadImage(Stream stream)
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(Guid.NewGuid().ToString(), stream)
            };
            var uploadResult = cloudinary.Upload(uploadParams);
            return uploadResult.Url.ToString();
        }
    }
}

