# Clear MongoDB Database
# This script clears the image_viewer database to resolve serialization issues

param(
    [string]$ConnectionString = "mongodb://localhost:27017",
    [string]$DatabaseName = "image_viewer"
)

Write-Host "🗑️ Clearing MongoDB database: $DatabaseName" -ForegroundColor Yellow

try {
    # Try to connect to MongoDB and clear the database
    $mongoPath = Get-Command mongo -ErrorAction SilentlyContinue
    if ($mongoPath) {
        $script = "use $DatabaseName; db.dropDatabase(); print('Database $DatabaseName cleared successfully');"
        & mongo --eval $script
    } else {
        # Try mongosh
        $mongoshPath = Get-Command mongosh -ErrorAction SilentlyContinue
        if ($mongoshPath) {
            $script = "use $DatabaseName; db.dropDatabase(); print('Database $DatabaseName cleared successfully');"
            & mongosh --eval $script
        } else {
            Write-Host "❌ MongoDB client not found. Please install MongoDB tools or clear the database manually." -ForegroundColor Red
            Write-Host "   You can also delete the database folder manually if using a local MongoDB instance." -ForegroundColor Yellow
            exit 1
        }
    }
    
    Write-Host "✅ Database cleared successfully" -ForegroundColor Green
} catch {
    Write-Host "❌ Error clearing database: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   The database may need to be cleared manually." -ForegroundColor Yellow
}
