# import-addresses.ps1

Zcash API works on top of the original Zcash node (zcashd), and uses it for tracking balances and history. 
To be able to track UTXO and transactions Zcash node must "know" address - so all observable addresses are imported into as "watch-only" addresses.
That's why when switching to the new Zcash node instance all current observable addresses must be re-imported to the node.

`import-addresses.ps1` script takes current observable addresses from database and imports them into Zcash node on chunk-by-chunk mode.

## Arguments

#### `-baseUrl <Base Zcash API url>`

Optional. By default is `http://zcash-api.lykke-service.svc.cluster.local`. Kubernetes VPN to the proper environment must be switched on for default value. 

#### `-chunkSize <Number of records to process by single step>`

Optional. By default is `100`.

## How to use

1. Ensure current node instance (if any) is stopped to prevent sending new transactions.
2. Set up new node instance (contact @Divlex for details).
3. Reconfigure Zcash API and job to use new node instance (is necessary only if new node is hosted on different URL/port).
4. Import addresses with help of `import-addresses.ps1`.
5. Restart node with `-rescan` option.
6. To ensure that all data was handled properly you may reset `LastBlockHeight` option in ZcashSettings table to any known past block hash.
   Zcash job re-handle transactions for all currently observable addresses since that block then.
   In some cases it's useful to reset `LastBlockHeight` to empty string in order to re-handle all transactions since first block.
7. In case when node is working but not syncing you may decide to switch to new node.
   It this case there are transactions that were successfully sent to node but not to blockchain may exist. Such transactions will be lost after switching to new node, and should be re-built and re-sent.