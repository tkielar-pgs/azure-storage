using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using PGS.Azure.Storage.Options;

namespace PGS.Azure.Storage.Controllers
{
    public class ImagesController : Controller
    {
        private readonly AzureStorageOptions _azureStorageOptions;
        private readonly AzureAdOptions _azureAdOptions;

        public ImagesController(IOptionsSnapshot<AzureStorageOptions> azureStorageOptions, IOptionsSnapshot<AzureAdOptions> azureAdOptions)
        {
            _azureAdOptions = azureAdOptions.Value;
            _azureStorageOptions = azureStorageOptions.Value;
        }

        // GET
        public async Task<IActionResult> Index()
        {
            var keyVault = new KeyVaultClient(async (authority, resource, scope) =>
            {
                var authContext = new AuthenticationContext(authority);
                var clientCred = new ClientCredential(_azureAdOptions.ClientId, _azureAdOptions.ClientSecret);
                var result = await authContext.AcquireTokenAsync(resource, clientCred);
                return result.AccessToken;
            });

            SecretBundle imgSas = await keyVault.GetSecretAsync(
                _azureStorageOptions.KeyVault.BaseUrl, $"{_azureStorageOptions.AccountName}-{_azureStorageOptions.KeyVault.ImgSasDefinitionName}");

            var account = new CloudStorageAccount(new StorageCredentials(_azureStorageOptions.AccountName, _azureStorageOptions.AccountKey), "core.windows.net", true);            
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(_azureStorageOptions.BlobContainerName);
            IListBlobItem[] blobs = await GetAllBlobs(container);
            string[] imageUrls = blobs.Select(blob => $"{blob.Uri}{imgSas.Value}").ToArray();

            return View(imageUrls);
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