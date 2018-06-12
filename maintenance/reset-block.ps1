##########################################################################
# Re-sets current block number for all deposit wallets
##########################################################################

<<<<<<< HEAD
param([string]$connectionString, [string]$blockchain = "Zcash", [string]$balanceTable = "EnrolledBalance", [int]$height = 0)
=======
param([string]$connectionString, [string]$blockchain = "Zcash", [long]$value = 0)

Start-Transcript
>>>>>>> 6470-fix-blocks

Import-Module AzureRmStorageTable

$ctx = New-AzureStorageContext -ConnectionString $connectionString

<<<<<<< HEAD
$storageTable = Get-AzureStorageTable –Name $balanceTable –Context $ctx

$entities = Get-AzureStorageTableRowByCustomFilter -table $storageTable -customFilter "(PartitionKey gt '$blockchain')"

foreach ($e in $entities) {
    $pk = $e.PartitionKey
    $rk = $e.RowKey
    $currentValue = $e.Block
    $e.Block = $height
    $e | Update-AzureStorageTableRow -table $storageTable > $null # avoid output to console
    Write-Host "$pk $rk $currentValue -> $height"
}

Write-Host "Done"
=======
#-------------------------------------------------------------------------
# Re-set enrolled balance block
#-------------------------------------------------------------------------

$enrolledBalanceTable = Get-AzureStorageTable –Name "EnrolledBalance" –Context $ctx

$entities = Get-AzureStorageTableRowByCustomFilter -table $enrolledBalanceTable -customFilter "(PartitionKey gt '$blockchain')"

Write-Host "EnrolledBalance:"

foreach ($e in $entities) {
    $currentValue = $e.Block
    $e.Block = $value
    $e | Update-AzureStorageTableRow -table $enrolledBalanceTable > $null # avoid output to console
    Write-Host "$($e.PartitionKey)`t$($e.RowKey)`tBalance: [$currentValue -> $value]"
}

#-------------------------------------------------------------------------
# Re-set cashin balance and transaction block
#-------------------------------------------------------------------------

# $cashinTable = Get-AzureStorageTable –Name "Cashin" –Context $ctx

# $entities = Get-AzureStorageTableRowByCustomFilter -table $cashinTable -customFilter "(BlockchainType eq '$blockchain')"

# Write-Host "Cashin:"

# foreach ($e in $entities) {
#     $currentBalanceBlockValue = $e.BalanceBlock
#     $currentTransactionBlockValue = $e.TransactionBlock
#     $e.BalanceBlock = $value
#     $e.TransactionBlock = $value
#     $e | Update-AzureStorageTableRow -table $enrolledBalanceTable > $null # avoid output to console
#     Write-Host "$($e.PartitionKey)`t$($e.RowKey)`tBalance: [$currentBalanceBlockValue -> $value]`tTransaction: [$currentTransactionBlockValue -> $value]"
# }

Write-Host "Done"

Stop-Transcript 
>>>>>>> 6470-fix-blocks
