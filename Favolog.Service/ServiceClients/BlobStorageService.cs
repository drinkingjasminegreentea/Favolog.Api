using Azure.Storage.Blobs;
using Favolog.Service.Settings;
using Microsoft.Extensions.Options;
using System;

namespace Favolog.Service.ServiceClients
{
    public class BlobStorageService: IBlobStorageService
    {
        private readonly AppSettings appSettings;

        public BlobStorageService(IOptions<AppSettings> options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            appSettings = options.Value;
        }

        public void UploadItemImageFromUrl(string sourceUrl, string blobName)
        {
            var blobServiceClient = new BlobServiceClient(appSettings.AzureBlobConnectionsString);
            var containerClient = blobServiceClient.GetBlobContainerClient("itemimages");
            var blockBlobClient = containerClient.GetBlobClient(blobName);
            blockBlobClient.SyncCopyFromUri(new Uri(sourceUrl));            
        }
    }
}
