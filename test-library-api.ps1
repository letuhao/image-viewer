#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test Library API with authentication

.DESCRIPTION
    This script logs in and creates a library with proper authentication

.EXAMPLE
    .\test-library-api.ps1 -Username "admin" -Password "admin123"
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$Username = "admin",
    
    [Parameter(Mandatory=$false)]
    [string]$Password = "admin123",
    
    [Parameter(Mandatory=$false)]
    [string]$ApiUrl = "https://localhost:11001",
    
    [Parameter(Mandatory=$false)]
    [string]$LibraryName = "E-Media",
    
    [Parameter(Mandatory=$false)]
    [string]$LibraryPath = "L:\test"
)

# Colors
$colors = @{
    Info = "Cyan"
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
}

function Write-Info($message) {
    Write-Host "[INFO] $message" -ForegroundColor $colors.Info
}

function Write-Success($message) {
    Write-Host "[SUCCESS] $message" -ForegroundColor $colors.Success
}

function Write-ErrorMsg($message) {
    Write-Host "[ERROR] $message" -ForegroundColor $colors.Error
}

# Ignore SSL certificate errors for localhost development
if ($PSVersionTable.PSVersion.Major -ge 6) {
    # PowerShell Core 6+
    $PSDefaultParameterValues['Invoke-RestMethod:SkipCertificateCheck'] = $true
    $PSDefaultParameterValues['Invoke-WebRequest:SkipCertificateCheck'] = $true
} else {
    # Windows PowerShell 5.1
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Library API Test Tool" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

try {
    # Step 1: Login
    Write-Info "Step 1: Logging in as '$Username'..."
    
    $loginBody = @{
        username = $Username
        password = $Password
    } | ConvertTo-Json
    
    $loginResponse = Invoke-RestMethod `
        -Uri "$ApiUrl/api/v1/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    $token = $loginResponse.token
    $userId = $loginResponse.user.id
    
    Write-Success "✓ Login successful!"
    Write-Info "  User ID: $userId"
    Write-Info "  Token: $($token.Substring(0, 20))..."
    Write-Host ""
    
    # Step 2: Create Library
    Write-Info "Step 2: Creating library '$LibraryName'..."
    
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }
    
    $libraryBody = @{
        name = $LibraryName
        path = $LibraryPath
        ownerId = $userId
        description = "Created via API test script"
        autoScan = $true
    } | ConvertTo-Json
    
    Write-Info "Request body:"
    Write-Host $libraryBody -ForegroundColor Gray
    Write-Host ""
    
    $libraryResponse = Invoke-RestMethod `
        -Uri "$ApiUrl/api/v1/libraries" `
        -Method Post `
        -Headers $headers `
        -Body $libraryBody `
        -ErrorAction Stop
    
    Write-Success "✓ Library created successfully!"
    Write-Host ""
    Write-Host "Library Details:" -ForegroundColor Yellow
    Write-Host "  ID: $($libraryResponse.id)" -ForegroundColor White
    Write-Host "  Name: $($libraryResponse.name)" -ForegroundColor White
    Write-Host "  Path: $($libraryResponse.path)" -ForegroundColor White
    Write-Host "  Auto Scan: $($libraryResponse.settings.autoScan)" -ForegroundColor White
    Write-Host ""
    
    # Step 3: Verify - Get all libraries
    Write-Info "Step 3: Verifying - fetching all libraries..."
    
    $libraries = Invoke-RestMethod `
        -Uri "$ApiUrl/api/v1/libraries" `
        -Method Get `
        -Headers $headers `
        -ErrorAction Stop
    
    Write-Success "✓ Found $($libraries.Count) library(ies)"
    foreach ($lib in $libraries) {
        Write-Host "  - $($lib.name) ($($lib.path))" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "======================================" -ForegroundColor Green
    Write-Host "  All tests passed! ✓" -ForegroundColor Green
    Write-Host "======================================" -ForegroundColor Green
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-ErrorMsg "Test failed!"
    Write-ErrorMsg "Error: $($_.Exception.Message)"
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-ErrorMsg "Status Code: $statusCode"
        
        # Try to read error response
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-ErrorMsg "Response: $responseBody"
        } catch {
            # Ignore
        }
    }
    
    Write-Host ""
    Write-Host "Common Issues:" -ForegroundColor Yellow
    Write-Host "  1. API not running - start with: .\start-all-services.bat" -ForegroundColor Gray
    Write-Host "  2. Wrong credentials - check username/password" -ForegroundColor Gray
    Write-Host "  3. Invalid path - ensure the path exists" -ForegroundColor Gray
    Write-Host ""
    
    exit 1
}

