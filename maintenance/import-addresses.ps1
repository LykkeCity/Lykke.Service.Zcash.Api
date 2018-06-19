##########################################################################
# Imports observable addresses from database to Zcash node.
# Uses Zcash API for all operations.
##########################################################################

param([string]$apiUrl = "http://zcash-api.lykke-service.svc.cluster.local", [string]$rpcUrl, [string]$rpcUsername, [string]$rpcPassword, [int]$chunkSize = 100)

function import([string]$apiUrl, [string]$rpcUrl, [string]$rpcUsername, [string]$rpcPassword, [string]$type, [int]$take) {
    $continuation = ""
    $count = 0
    $apiUrl = $apiUrl.TrimEnd("/")

    do {
        $chunk = Invoke-WebRequest "$apiUrl/api/addresses?type=$type&take=$take&continuation=$continuation" | ConvertFrom-Json

        if ($chunk.items.Count -gt 0) {
            $count = $count + $chunk.items.Count

            if ($rpcUrl) {
                $headers = @{
                    Authorization = "Basic " + [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($rpcUsername + ":" + $rpcPassword));
                }

                for ($i = 0; $i -lt $chunk.items.Count; $i++) {
                    $body = @{ 
                        jsonrpc = "2.0"; 
                        id      = "$i"; 
                        method  = "validateaddress"; 
                        params  = @($chunk.items[$i]);
                    }

                    $resp = Invoke-WebRequest $rpcUrl -Method Post -Body ($body | ConvertTo-Json) -Headers $headers -ContentType "application/json" | ConvertFrom-Json

                    if ($resp.result.isvalid -and !$resp.result.ismine -and !$resp.result.iswatchonly) {
                        $body.method = "importaddress"
                        Invoke-WebRequest $rpcUrl -Method Post -Body ($body | ConvertTo-Json) -Headers $headers -ContentType "application/json" > $null
                    }
                }
            }
            else {
                $items = ConvertTo-Json $chunk.items
                Invoke-WebRequest "$apiUrl/api/addresses/import" -Method Post -Body $items -ContentType "application/json" > $null
            }
        }

        $continuation = $chunk.continuation
    
        Write-Host "`r$count $type addresses imported" -NoNewline
    }
    while (($continuation -ne $null) -and ($continuation -ne ''))

    Write-Host ""
}

# it seems like "-ErrorAction Stop" not works properly with "Invoke-WebRequest"
# so wrap script in try-catch-throw to break execution on error
try {
    import $apiUrl $rpcUrl $rpcUsername $rpcPassword 'balance' $chunkSize -ErrorAction Stop
    import $apiUrl $rpcUrl $rpcUsername $rpcPassword 'history' $chunkSize -ErrorAction Stop

    Write-Host "Done"
}
catch {
    throw
}