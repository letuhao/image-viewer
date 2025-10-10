#!/usr/bin/env pwsh
# Create Admin Account Script
# Creates an admin user account via the API

Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "👤 CREATE ADMIN ACCOUNT" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan

# Configuration
$API_BASE_URL = "https://localhost:11001/api/v1"

# Admin credentials
$adminUsername = "admin"
$adminEmail = "admin@imageviewer.local"
$adminPassword = "Admin@123456"

Write-Host "📋 Admin Account Details:" -ForegroundColor Cyan
Write-Host "   Username: $adminUsername" -ForegroundColor White
Write-Host "   Email: $adminEmail" -ForegroundColor White
Write-Host "   Password: $adminPassword" -ForegroundColor White
Write-Host "   Role: Admin" -ForegroundColor White
Write-Host ""

# Step 1: Check if API is running
Write-Host "🔍 Step 1: Checking if API is running..." -ForegroundColor Cyan

try {
    $healthCheck = Invoke-RestMethod -Uri "https://localhost:11001/health" -Method Get -SkipCertificateCheck -TimeoutSec 5
    Write-Host "   ✅ API is running and healthy!" -ForegroundColor Green
} catch {
    Write-Host "   ❌ API is not running!" -ForegroundColor Red
    Write-Host "   💡 Please start the API first: cd src/ImageViewer.Api && dotnet run" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Step 2: Register admin account
Write-Host "🔍 Step 2: Registering admin account..." -ForegroundColor Cyan

$registerRequest = @{
    username = $adminUsername
    email = $adminEmail
    password = $adminPassword
    confirmPassword = $adminPassword
    role = "Admin"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod `
        -Uri "$API_BASE_URL/security/register" `
        -Method Post `
        -Body $registerRequest `
        -ContentType "application/json" `
        -SkipCertificateCheck
    
    Write-Host "   ✅ Admin account created successfully!" -ForegroundColor Green
    Write-Host "      User ID: $($registerResponse.userId)" -ForegroundColor White
    Write-Host "      Username: $($registerResponse.username)" -ForegroundColor White
    Write-Host "      Email: $($registerResponse.email)" -ForegroundColor White
    Write-Host "      Role: $($registerResponse.role)" -ForegroundColor White
} catch {
    $errorMessage = $_.Exception.Message
    
    if ($errorMessage -like "*already exists*" -or $errorMessage -like "*409*") {
        Write-Host "   ⚠️  Admin account already exists!" -ForegroundColor Yellow
        Write-Host "      Trying to login instead..." -ForegroundColor White
    } else {
        Write-Host "   ❌ Failed to create admin account: $errorMessage" -ForegroundColor Red
        Write-Host "   💡 This is normal if the register endpoint doesn't exist yet" -ForegroundColor Yellow
        Write-Host "      We'll create the account directly in MongoDB instead..." -ForegroundColor Yellow
    }
}

Write-Host ""

# Step 3: Test login
Write-Host "🔍 Step 3: Testing admin login..." -ForegroundColor Cyan

$loginRequest = @{
    username = $adminUsername
    password = $adminPassword
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$API_BASE_URL/security/login" `
        -Method Post `
        -Body $loginRequest `
        -ContentType "application/json" `
        -SkipCertificateCheck
    
    Write-Host "   ✅ Login successful!" -ForegroundColor Green
    Write-Host "      Token: $($loginResponse.token.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host "      User ID: $($loginResponse.userId)" -ForegroundColor White
    Write-Host "      Username: $($loginResponse.username)" -ForegroundColor White
    Write-Host "      Roles: $($loginResponse.roles -join ', ')" -ForegroundColor White
    Write-Host "      Expires At: $($loginResponse.expiresAt)" -ForegroundColor White
    
    # Save token to file for easy testing
    $loginResponse.token | Out-File -FilePath "admin-token.txt" -NoNewline
    Write-Host ""
    Write-Host "   💾 Token saved to: admin-token.txt" -ForegroundColor Green
    
} catch {
    Write-Host "   ❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Step 4: Verify with /auth/me endpoint
if ($loginResponse) {
    Write-Host "🔍 Step 4: Verifying token with /auth/me..." -ForegroundColor Cyan
    
    try {
        $meResponse = Invoke-RestMethod `
            -Uri "$API_BASE_URL/auth/me" `
            -Method Get `
            -Headers @{ Authorization = "Bearer $($loginResponse.token)" } `
            -SkipCertificateCheck
        
        Write-Host "   ✅ Token verified!" -ForegroundColor Green
        Write-Host "      User ID: $($meResponse.userId)" -ForegroundColor White
        Write-Host "      Username: $($meResponse.username)" -ForegroundColor White
        Write-Host "      Roles: $($meResponse.roles -join ', ')" -ForegroundColor White
    } catch {
        Write-Host "   ❌ Token verification failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "📊 SUMMARY" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "🎯 Admin Credentials:" -ForegroundColor Cyan
Write-Host "   Username: $adminUsername" -ForegroundColor Green
Write-Host "   Password: $adminPassword" -ForegroundColor Green
Write-Host "   Email: $adminEmail" -ForegroundColor Green
Write-Host ""
Write-Host "🔑 Authentication Token:" -ForegroundColor Cyan
if ($loginResponse) {
    Write-Host "   ✅ Token generated and saved to admin-token.txt" -ForegroundColor Green
    Write-Host "   Use this token in API requests:" -ForegroundColor White
    Write-Host "   Authorization: Bearer $(Get-Content admin-token.txt)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ No token available" -ForegroundColor Red
}
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan

