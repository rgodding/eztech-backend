using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EzTech.Api.Services;

public interface IBlobService
{
    Task<Stream> GetImage(string url);
    Task<(int, string)> UploadImage(string fileName, IFormFile file);
    Task<(int, string)> DeleteImage(string url);
    Task<bool> ImageExists(string filename);

    int GetBlobCount();

    // Used during development to wipe the blob storage
    Task<bool> Wipe();
}

public class BlobService : IBlobService
{
    private readonly IConfiguration _configuration;

    public BlobService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<Stream> GetImage(string filename)
    {
        if (filename == null)
        {
            throw new Exception("Filename is null");
        }

        var containerClient = GetBlobContainerClient();
        try
        {
            var blobClient = containerClient.GetBlobClient(filename);
            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }
        catch (Exception e)
        {
            // Instead we return a default image, that is stored locally
            var defaultImage = await File.ReadAllBytesAsync("wwwroot/images/no-image.jpg");
            return new MemoryStream(defaultImage);
        }
    }

    public async Task<(int, string)> UploadImage(string fileName, IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        var imageExists = await ImageExists(fileName);
        if (imageExists)
        {
            return (0, "Image already exists");
        }

        var containerClient = GetBlobContainerClient();
        var blobClient = containerClient.GetBlobClient(fileName);
        var blobHttpHeader = new BlobHttpHeaders
        {
            ContentType = "image/png"
        };
        var response = await blobClient.UploadAsync(new MemoryStream(fileBytes), blobHttpHeader);
        if (response.GetRawResponse().Status == 201)
        {
            return (1, fileName);
        }

        return (0, "Image not uploaded");
    }

    public async Task<(int, string)> DeleteImage(string url)
    {
        var containerClient = GetBlobContainerClient();
        var blobClient = containerClient.GetBlobClient(url);
        var response = await blobClient.DeleteIfExistsAsync();
        if (response.Value)
        {
            return (1, "Image deleted");
        }

        return (0, "Image not found");
    }

    public async Task<bool> ImageExists(string filename)
    {
        var containerClient = GetBlobContainerClient();
        var blobClient = containerClient.GetBlobClient(filename);
        return await blobClient.ExistsAsync();
    }

    public int GetBlobCount()
    {
        var containerClient = GetBlobContainerClient();
        var blobs = containerClient.GetBlobs();
        return blobs.Count();
    }

    public Task<bool> Wipe()
    {
        var containerClient = GetBlobContainerClient();
        var blobs = containerClient.GetBlobs();
        foreach (var blob in blobs)
        {
            var blobClient = containerClient.GetBlobClient(blob.Name);
            blobClient.DeleteIfExists();
        }

        return Task.FromResult(true);
    }


    private BlobContainerClient GetBlobContainerClient()
    {
        var connectionString = _configuration["Azurite:ConnectionString"];
        var containerName = _configuration["Azurite:Container"];
        var blobServiceClient = new BlobServiceClient(connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        containerClient.CreateIfNotExists();
        return containerClient;
    }
}