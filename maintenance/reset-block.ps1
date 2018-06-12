##########################################################################
# Re-sets current block number for all deposit wallets
##########################################################################

param([string]$connectionString, [string]$blockchain = "Zcash", [long]$value = 0)

Start-Transcript

Import-Module AzureRmStorageTable

$ctx = New-AzureStorageContext -ConnectionString $connectionString

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
