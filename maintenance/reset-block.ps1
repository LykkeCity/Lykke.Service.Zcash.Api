##########################################################################
# Re-sets current block number for all deposit wallets
##########################################################################

param([string]$connectionString, [string]$blockchain = "Zcash", [string]$balanceTable = "EnrolledBalance", [int]$height = 0)

Import-Module AzureRmStorageTable

$ctx = New-AzureStorageContext -ConnectionString $connectionString

$storageTable = Get-AzureStorageTable –Name $balanceTable –Context $ctx

$entities = Get-AzureStorageTableRowByCustomFilter -table $storageTable -customFilter "(PartitionKey gt '$blockchain')"

foreach ($e in $entities) {
    $pk = $e.PartitionKey
    $rk = $e.RowKey
    $currentValue = $e.Block
    $e.Block = $height
    $e | Update-AzureStorageTableRow -table $storageTable
    Write-Host "$pk $rk $currentValue -> $height"
}

Write-Host "Done"