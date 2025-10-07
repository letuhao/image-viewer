# Bulk Add Collections from ZIP Files
# This script performs bulk operations to add collections from ZIP files

param(
    [string]$SourcePath = "L:\EMedia\AI_Generated\Geldoru",
    [string]$ApiBaseUrl = "http://localhost:11000/api/v1",
    [int]$MaxWaitTime = 300
)

Write-Host "📦 Starting bulk collection addition..." -ForegroundColor Green

# Get ZIP files from source directory
Write-Host "🔍 Scanning for ZIP files in: $SourcePath" -ForegroundColor Yellow
$zipFiles = Get-ChildItem -Path $SourcePath -Filter "*.zip" | Sort-Object Name

if ($zipFiles.Count -eq 0) {
    Write-Host "❌ No ZIP files found in $SourcePath" -ForegroundColor Red
    exit 1
}

Write-Host "📋 Found $($zipFiles.Count) ZIP files:" -ForegroundColor Cyan
$zipFiles | ForEach-Object { Write-Host "   📄 $($_.Name)" -ForegroundColor Gray }

# Prepare bulk operation request
$bulkRequest = @{
    parentPath = $SourcePath
    collectionPrefix = ""
    includeSubfolders = $false
    autoAdd = $true
    overwriteExisting = $false
    processCompressedFiles = $true
    maxConcurrentOperations = 5
}

Write-Host "`n🚀 Starting bulk operation..." -ForegroundColor Yellow
Write-Host "📊 Request details:" -ForegroundColor Cyan
Write-Host "   📁 Source Path: $($bulkRequest.parentPath)" -ForegroundColor Gray
Write-Host "   🔄 Process Compressed: $($bulkRequest.processCompressedFiles)" -ForegroundColor Gray
Write-Host "   ⚡ Max Concurrent: $($bulkRequest.maxConcurrentOperations)" -ForegroundColor Gray

try {
    # Start bulk operation
    $bulkResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/bulk/collections" -Method POST -Body ($bulkRequest | ConvertTo-Json) -ContentType "application/json" -TimeoutSec 30
    
    if ($bulkResponse -and $bulkResponse.OperationId) {
        $operationId = $bulkResponse.OperationId
        Write-Host "✅ Bulk operation started successfully!" -ForegroundColor Green
        Write-Host "🆔 Operation ID: $operationId" -ForegroundColor Cyan
        
        # Monitor operation progress
        Write-Host "`n⏳ Monitoring operation progress..." -ForegroundColor Yellow
        $startTime = Get-Date
        $operationComplete = $false
        
        while (-not $operationComplete -and ((Get-Date) - $startTime).TotalSeconds -lt $MaxWaitTime) {
            try {
                $statusResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/bulk/status/$operationId" -Method GET -TimeoutSec 10
                
                if ($statusResponse) {
                    $progress = if ($statusResponse.TotalItems -gt 0) { [math]::Round(($statusResponse.ProcessedItems / $statusResponse.TotalItems) * 100, 1) } else { 0 }
                    Write-Host "📊 Progress: $($statusResponse.ProcessedItems)/$($statusResponse.TotalItems) ($progress%) - Status: $($statusResponse.Status)" -ForegroundColor Cyan
                    
                    if ($statusResponse.Status -eq "Completed") {
                        $operationComplete = $true
                        Write-Host "✅ Bulk operation completed successfully!" -ForegroundColor Green
                        
                        # Display results
                        Write-Host "`n📈 Operation Results:" -ForegroundColor Cyan
                        Write-Host "   ✅ Successful: $($statusResponse.SuccessfulItems)" -ForegroundColor Green
                        Write-Host "   ❌ Failed: $($statusResponse.FailedItems)" -ForegroundColor Red
                        Write-Host "   ⏱️ Duration: $($statusResponse.Duration)" -ForegroundColor Gray
                        
                        if ($statusResponse.Errors -and $statusResponse.Errors.Count -gt 0) {
                            Write-Host "`n❌ Errors encountered:" -ForegroundColor Red
                            $statusResponse.Errors | ForEach-Object { Write-Host "   • $_" -ForegroundColor Red }
                        }
                    }
                    elseif ($statusResponse.Status -eq "Failed") {
                        Write-Host "❌ Bulk operation failed!" -ForegroundColor Red
                        if ($statusResponse.Errors) {
                            $statusResponse.Errors | ForEach-Object { Write-Host "   • $_" -ForegroundColor Red }
                        }
                        exit 1
                    }
                }
            }
            catch {
                Write-Host "⚠️ Error checking operation status: $($_.Exception.Message)" -ForegroundColor Yellow
            }
            
            if (-not $operationComplete) {
                Start-Sleep -Seconds 5
            }
        }
        
        if (-not $operationComplete) {
            Write-Host "⏰ Operation did not complete within $MaxWaitTime seconds" -ForegroundColor Yellow
            Write-Host "🔄 Operation may still be running in background" -ForegroundColor Yellow
        }
        
        return @{
            Success = $operationComplete
            OperationId = $operationId
            Status = $statusResponse
            ZipFiles = $zipFiles
        }
    }
    else {
        Write-Host "❌ Failed to start bulk operation" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ Bulk operation failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "🔍 Response: $($_.Exception.Response)" -ForegroundColor Red
    exit 1
}
