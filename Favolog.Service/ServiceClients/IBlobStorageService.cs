using Azure.Storage.Blobs.Models;

namespace Favolog.Service.ServiceClients
{
    public interface IBlobStorageService
    {
        void UploadItemImageFromUrl(string sourceUrl, string blobName);
    }
}
