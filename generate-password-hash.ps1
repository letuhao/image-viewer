#!/usr/bin/env pwsh
# Generate BCrypt Password Hash
# Generates a BCrypt hash for the admin password using the same settings as the application

Write-Host "`n🔐 Generating BCrypt Password Hash..." -ForegroundColor Cyan

$password = "Admin@123456"

# Create a simple C# console app to hash the password
$csharpCode = @"
using BCrypt.Net;
using System;

class Program
{
    static void Main()
    {
        string password = "$password";
        string hash = BCrypt.Net.BCrypt.HashPassword(password, 12);
        Console.WriteLine(hash);
    }
}
"@

# Save to temp file
$tempDir = Join-Path $env:TEMP "password-hash-$(Get-Random)"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

$csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>
</Project>
"@

$csharpCode | Out-File -FilePath "$tempDir/Program.cs" -Encoding UTF8
$csprojContent | Out-File -FilePath "$tempDir/HashGenerator.csproj" -Encoding UTF8

Write-Host "   Building hash generator..." -ForegroundColor Yellow
$buildOutput = & dotnet build "$tempDir/HashGenerator.csproj" --nologo --verbosity quiet 2>&1

Write-Host "   Generating hash..." -ForegroundColor Yellow
$hash = & dotnet run --project "$tempDir/HashGenerator.csproj" --no-build --nologo 2>&1 | Select-Object -Last 1

# Cleanup
Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue

if ($hash) {
    Write-Host "`n✅ Password hash generated!" -ForegroundColor Green
    Write-Host "   Password: $password" -ForegroundColor White
    Write-Host "   Hash: $hash" -ForegroundColor Gray
    Write-Host ""
    
    # Save to file
    $hash | Out-File -FilePath "admin-password-hash.txt" -NoNewline
    Write-Host "   💾 Hash saved to: admin-password-hash.txt" -ForegroundColor Green
    Write-Host ""
    
    return $hash
} else {
    Write-Host "`n❌ Failed to generate hash" -ForegroundColor Red
}

