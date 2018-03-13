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