using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using SyncSyntax.Settings;
using System.Text.RegularExpressions;

namespace SyncSyntax.Services
{
    public class CloudinaryImageService
    {
        private readonly Cloudinary _cloudinary;

        private readonly string[] _allowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        public CloudinaryImageService(
            IOptions<CloudinarySettings> settings)
        {
            var cloudinarySettings = settings.Value;

            if (string.IsNullOrWhiteSpace(
                    cloudinarySettings.CloudName) ||
                string.IsNullOrWhiteSpace(
                    cloudinarySettings.ApiKey) ||
                string.IsNullOrWhiteSpace(
                    cloudinarySettings.ApiSecret))
            {
                throw new InvalidOperationException(
                    "Cloudinary settings are missing.");
            }

            var account = new Account(
                cloudinarySettings.CloudName,
                cloudinarySettings.ApiKey,
                cloudinarySettings.ApiSecret);

            _cloudinary = new Cloudinary(account);

            _cloudinary.Api.Secure = true;
        }

        public async Task<ImageUploadResultData>
            UploadImageAsync(
                IFormFile file,
                string? postTitle)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException(
                    "The selected image is empty.");
            }

            const long maximumFileSize =
                5 * 1024 * 1024;

            if (file.Length > maximumFileSize)
            {
                throw new InvalidOperationException(
                    "The image must be smaller than 5 MB.");
            }

            var extension = Path
                .GetExtension(file.FileName)
                .ToLowerInvariant();

            if (!_allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Only JPG, JPEG and PNG images are allowed.");
            }

            var safeName = CreateSafeImageName(
                postTitle,
                file.FileName);

            await using var stream =
                file.OpenReadStream();

            var uploadParameters =
                new ImageUploadParams
                {
                    File = new FileDescription(
                        file.FileName,
                        stream),

                    Folder = "blog-posts",

                    PublicId = safeName,

                    Overwrite = false,

                    UniqueFilename = true,

                    UseFilename = false
                };

            var result = await _cloudinary
                .UploadAsync(uploadParameters);

            if (result.Error != null)
            {
                throw new InvalidOperationException(
                    result.Error.Message);
            }

            if (result.SecureUrl == null)
            {
                throw new InvalidOperationException(
                    "Cloudinary did not return an image URL.");
            }

            return new ImageUploadResultData
            {
                ImageUrl =
                    result.SecureUrl.ToString(),

                PublicId =
                    result.PublicId
            };
        }

        public async Task DeleteImageAsync(
            string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return;
            }

            var deleteParameters =
                new DeletionParams(publicId)
                {
                    ResourceType =
                        ResourceType.Image,

                    Invalidate = true
                };

            await _cloudinary.DestroyAsync(
                deleteParameters);
        }

        private static string CreateSafeImageName(
            string? postTitle,
            string originalFileName)
        {
            var sourceName =
                !string.IsNullOrWhiteSpace(postTitle)
                    ? postTitle
                    : Path.GetFileNameWithoutExtension(
                        originalFileName);

            sourceName =
                sourceName.Trim().ToLowerInvariant();

            sourceName = Regex.Replace(
                sourceName,
                @"[^a-z0-9]+",
                "-");

            sourceName =
                sourceName.Trim('-');

            if (string.IsNullOrWhiteSpace(sourceName))
            {
                sourceName = "post-image";
            }

            return $"{sourceName}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }
    }

    public class ImageUploadResultData
    {
        public string ImageUrl { get; set; } =
            string.Empty;

        public string PublicId { get; set; } =
            string.Empty;
    }
}