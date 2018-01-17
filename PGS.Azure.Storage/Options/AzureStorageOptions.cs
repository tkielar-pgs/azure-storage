namespace PGS.Azure.Storage.Options
{
    public class AzureStorageOptions
    {
        public string AccountName { get; set; }
        public string BlobContainerName { get; set; }
        public AzureStorageKeyVaultOptions KeyVault { get; set; }
    }
}