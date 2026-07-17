using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using SyncSyntax.Settings;

namespace SyncSyntax.Services
{
    public class CloudinaryImageService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryImageService(
            IOptions<CloudinarySettings> options)
        {
            var settings = options.Value;

            if (string.IsNullOrWhiteSpace(settings.CloudName))
            {
                throw new InvalidOperationException(
                    "Cloudinary CloudName is missing.");
            }

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new InvalidOperationException(
                    "Cloudinary ApiKey is missing.");
            }

            if (string.IsNullOrWhiteSpace(settings.ApiSecret))
            {
                throw new InvalidOperationException(
                    "Cloudinary ApiSecret is missing.");
            }

            var account = new Account(
                settings.CloudName.Trim(),
                settings.ApiKey.Trim(),
                settings.ApiSecret.Trim());

            _cloudinary = new Cloudinary(account);

            _cloudinary.Api.Secure = true;
        }

        public async Task<(string ImageUrl, string PublicId)>
            UploadImageAsync(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                throw new InvalidOperationException(
                    "Please select an image.");
            }

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            var extension =
                Path.GetExtension(image.FileName)
                    .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Only JPG, JPEG, PNG and WEBP files are allowed.");
            }

            await using var stream =
                image.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(
                    image.FileName,
                    stream),

                Folder = "syncsyntax/posts",

                UseFilename = true,

                UniqueFilename = true,

                Overwrite = false
            };

            var uploadResult =
                await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new InvalidOperationException(
                    "Cloudinary error: " +
                    uploadResult.Error.Message);
            }

            if (uploadResult.SecureUrl == null)
            {
                throw new InvalidOperationException(
                    "Cloudinary did not return an image URL.");
            }

            Console.WriteLine(
                "Cloudinary upload successful.");

            Console.WriteLine(
                $"Image URL: {uploadResult.SecureUrl}");

            Console.WriteLine(
                $"Public ID: {uploadResult.PublicId}");

            return (
                uploadResult.SecureUrl.ToString(),
                uploadResult.PublicId);
        }

        public async Task DeleteImageAsync(
            string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return;
            }

            var deleteParams =
                new DeletionParams(publicId)
                {
                    ResourceType =
                        ResourceType.Image
                };

            await _cloudinary.DestroyAsync(
                deleteParams);
        }
    }
}