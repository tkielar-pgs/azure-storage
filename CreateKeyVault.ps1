param(
    [Parameter(Mandatory=$true)][string]$ResourceGroupName,
    [Parameter(Mandatory=$true)][string]$KeyVaultName,
    [Parameter(Mandatory=$true)][string]$StorageAccountName
)

$resourceGroup = Get-AzureRmResourceGroup -Name $ResourceGroupName

$keyVault = New-AzureRmKeyVault -VaultName $KeyVaultName -ResourceGroupName $resourceGroup.ResourceGroupName -Location $resourceGroup.Location

$keyVaultPrincipal = Get-AzureRmADServicePrincipal | Where-Object {$_.DisplayName -eq 'Azure Key Vault'}

$storageAccount = Get-AzureRmStorageAccount -ResourceGroupName $resourceGroup.ResourceGroupName -Name $StorageAccountName

$roleAssignment = New-AzureRmRoleAssignment -ObjectId $keyVaultPrincipal.Id -RoleDefinitionName 'Storage Account Key Operator Service Role' -Scope $storageAccount.Id

$managedStorageAccount = Add-AzureKeyVaultManagedStorageAccount -VaultName $keyVault.VaultName -AccountName $storageAccount.StorageAccountName -AccountResourceId $storageAccount.Id -ActiveKeyName key1 `
    -RegenerationPeriod ([System.TimeSpan]::FromDays(1))

$sasDefinition = Set-AzureKeyVaultManagedStorageSasDefinition -AccountName $managedStorageAccount.AccountName -Container img -Name imgsas -Policy img-policy -VaultName $keyVault.VaultName

