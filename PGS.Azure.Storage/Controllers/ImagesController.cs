using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using PGS.Azure.Storage.Options;

namespace PGS.Azure.Storage.Controllers
{
    public class ImagesController : Controller
    {
        private readonly AzureStorageOptions _azureStorageOptions;

        public ImagesController(IOptionsSnapshot<AzureStorageOptions> azureStorageOptions) => _azureStorageOptions = azureStorageOptions.Value;

        // GET
        public async Task<IActionResult> Index()
        {
            var account = new CloudStorageAccount(new StorageCredentials(), _azureStorageOptions.AccountName, "core.windows.net", true);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(_azureStorageOptions.BlobContainerName);
            IListBlobItem[] blobs = await GetAllBlobs(container);            

            return View(blobs.Select(blob => blob.Uri.AbsoluteUri).ToArray());
        }

        private async Task<IListBlobItem[]> GetAllBlobs(CloudBlobContainer container)
        {
            var result = new List<IListBlobItem>();
            BlobContinuationToken continuationToken = null;

            do
            {
                BlobResultSegment blobs = await container.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = blobs.ContinuationToken;
                result.AddRange(blobs.Results);                
            }
            while (continuationToken != null);

            return result.ToArray();
        }
    }
}