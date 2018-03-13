##########################################################################
# Imports observable addresses from database to Zcash node.
# Uses Zcash API for all operations.
##########################################################################

param([string]$baseUrl = "http://zcash-api.lykke-service.svc.cluster.local", [int]$chunkSize = 100)

function import([string]$url, [string]$type, [int]$take)
{
    $continuation = ""
    $count = 0
    $url = $url.TrimEnd("/")

    do
    {
        $chunk = Invoke-WebRequest "$url/api/addresses?type=$type&take=$take&continuation=$continuation" | ConvertFrom-Json

        if ($chunk.items.Count -gt 0)
        {
            $count = $count + $chunk.items.Count
            $items = ConvertTo-Json $chunk.items
            Invoke-WebRequest "$url/api/addresses/import" -Method Post -Body $items -ContentType "application/json" > $null # avoid output to console
        }

        $continuation = $chunk.continuation
        
        Write-Host "`r$count $type addresses imported" -NoNewline
    }
    while(($continuation -ne $null) -and ($continuation -ne ''))

    Write-Host ""
}

# it seems like "-ErrorAction Stop" not works properly with "Invoke-WebRequest"
# so wrap script in try-catch-throw to break execution on error
try 
{
    import $baseUrl 'balance' $chunkSize -ErrorAction Stop
    import $baseUrl 'history' $chunkSize -ErrorAction Stop

    Write-Host "Done"
}
catch 
{
    throw
}