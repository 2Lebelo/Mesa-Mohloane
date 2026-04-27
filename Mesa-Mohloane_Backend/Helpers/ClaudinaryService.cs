using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace Mesa_Mohloane_Backend.Helpers;

public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinarySettings> settings)
    {
        var s = settings.Value;
        var account = new Account(s.CloudName, s.ApiKey, s.ApiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    /// <summary>
    /// Uploads an image file to Cloudinary under the given folder.
    /// Returns (imageUrl, publicId) on success.
    /// </summary>
    public async Task<(string ImageUrl, string PublicId)> UploadAsync(
        IFormFile file, string folder = "incidents")
    {
        if (file.Length == 0)
            throw new ArgumentException("File is empty.");

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            Transformation = new Transformation()
                .Quality("auto")
                .FetchFormat("auto")
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error is not null)
            throw new InvalidOperationException(
                $"Cloudinary upload failed: {result.Error.Message}");

        return (result.SecureUrl.ToString(), result.PublicId);
    }

    /// <summary>
    /// Deletes an image from Cloudinary by its public ID.
    /// </summary>
    public async Task DeleteAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
    }
}