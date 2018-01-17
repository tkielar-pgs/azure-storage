using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
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
            var keyVault = CreateKeyVaultClient();

            string containerSas = await GetKvSas(keyVault, _azureStorageOptions.KeyVault.ContainerSasDefinitionName);
            var account = new CloudStorageAccount(new StorageCredentials(containerSas), _azureStorageOptions.AccountName, "core.windows.net", true);       
            
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(_azureStorageOptions.BlobContainerName);
            IListBlobItem[] blobs = await GetAllBlobs(container);

            string imgSas = await GetKvSas(keyVault, _azureStorageOptions.KeyVault.ImgSasDefinitionName);
            string[] imageUrls = blobs.Select(blob => $"{blob.Uri}{imgSas}").ToArray();

            return View(imageUrls);
        }

        private KeyVaultClient CreateKeyVaultClient()
        {
            if (string.IsNullOrWhiteSpace(_azureAdOptions.ClientId) || string.IsNullOrWhiteSpace(_azureAdOptions.ClientSecret))
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                return new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            }

            return new KeyVaultClient(async (authority, resource, scope) =>
            {
                var authContext = new AuthenticationContext(authority);
                var clientCred = new ClientCredential(_azureAdOptions.ClientId, _azureAdOptions.ClientSecret);
                var result = await authContext.AcquireTokenAsync(resource, clientCred);
                return result.AccessToken;
            });
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

        private async Task<string> GetKvSas(KeyVaultClient kvClient, string sasDefinitionName)
        {
            SecretBundle secret = await kvClient.GetSecretAsync(_azureStorageOptions.KeyVault.BaseUrl, $"{_azureStorageOptions.AccountName}-{sasDefinitionName}");
            return secret.Value;
        }
    }
}