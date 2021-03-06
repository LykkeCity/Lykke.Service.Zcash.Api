# import-addresses.ps1

Zcash API works on top of the original Zcash node (zcashd), and uses it for tracking balances and history. 
To be able to track UTXO and transactions Zcash node must "know" address - so all observable addresses are imported into as "watch-only" addresses.
That's why when switching to the new Zcash node instance all current observable addresses must be re-imported to the node.

`import-addresses.ps1` script takes current observable addresses from database and imports them into Zcash node on chunk-by-chunk mode.

## Arguments

#### `-apiUrl <Base Zcash API url>`

Optional. By default is `http://zcash-api.lykke-service.svc.cluster.local`. Kubernetes VPN to the proper environment must be switched on for default value.

#### `-rpcUrl <Zcash JSON RPC url>`

Optional (recommended).
If specified then direct import from API to node through JSON RPC is performed (i.e. it may be required to initialize staging instance of Zcash node).
If not specified then addresses are re-imported into currently configured node through API.

#### `-rpcUsername <Zcash JSON RPC username>`

Optional (recommended, see `-rpcUrl`).

#### `-rpcPassword <Zcash JSON RPC password>`

Optional (recommended, see `-rpcUrl`).

#### `-chunkSize <Number of records to process by single step>`

Optional. By default is `100`.

## How to use

1. Set up new node instance.
2. Import addresses with help of `import-addresses.ps1`.
3. Restart node with `-rescan` option.
4. Reconfigure Zcash API and job to use new node instance.

Also to ensure that all data was handled properly you may reset `LastBlockHeight` option in ZcashSettings table to any known past block hash.
Zcash job re-handle transactions for all currently observable addresses since that block then.
In some cases it's useful to reset `LastBlockHeight` to empty string in order to re-handle all transactions since first block.

In case when node is working but not syncing you may decide to switch to new node.
It this case there are transactions that were successfully sent to node but not to blockchain may exist.
Such transactions will be lost after switching to new node, and should be re-built and re-sent.

# reset-block.ps1

Initially block timestamp used to control operations and balances instead of real block height.
To switch to using real block height we need to re-set Block value for all deposit addresses.

## Pre-requesities

Run PowerShell cmd-lets listed below to install modules required to work with Azure Tables:

`Install-Module PowerShellGet -Force`

`Install-Module -Name AzureRM -Repository PSGallery -Force`

`Install-Module AzureRmStorageTable`

## Arguments

#### `-connectionString <Connection string to Azure Storage Account>`

Required, must be quoted. Connection string to storage account with `EnrolledBalance` table. 

#### `-blockchain <Blockchain name>`

Optional. By default is `Zcash`.

#### `-value <New Block field value>`

Optional. By default is `0`.

#### `-minCashin <Minimal cashin amount>`

Optional. By default is `0`. If current enrolled balance is greater than minimal cashin then row is skipped and balance block is not reset.
