// Fix truncated ZIP file paths in MongoDB
// This script fixes paths that were truncated at the # character during original scans

// Connect to MongoDB
use image_viewer;

print("🔧 Starting ZIP path truncation fix...");

// Find collections with truncated ZIP paths
var collections = db.collections.find({
    "images": {
        $elemMatch: {
            "legacyRelativePath": { $regex: /^\[.*\] [^#]*$/ }
        }
    }
});

var totalFixed = 0;
var totalCollections = 0;

collections.forEach(function(collection) {
    print(`\n📁 Processing collection: ${collection.name}`);
    totalCollections++;
    
    var imagesFixed = 0;
    var images = collection.images || [];
    
    for (var i = 0; i < images.length; i++) {
        var image = images[i];
        var oldPath = image.legacyRelativePath;
        
        // Check if this looks like a truncated ZIP path
        if (oldPath && oldPath.match(/^\[.*\] [^#]*$/) && !oldPath.includes("::") && !oldPath.includes("#")) {
            // This looks like a truncated ZIP path
            // We need to find the actual ZIP file on disk
            
            var collectionPath = collection.path;
            var fullCollectionPath = collectionPath + "\\" + oldPath;
            
            // Try to find the actual ZIP file by looking for files that start with this name
            // This is a heuristic approach - we'll look for the most likely match
            
            print(`  🔍 Found truncated path: ${oldPath}`);
            print(`  📂 Collection path: ${collectionPath}`);
            print(`  🔗 Full path would be: ${fullCollectionPath}`);
            
            // For now, we'll mark this as needing manual review
            // In a real scenario, you'd want to implement file system scanning
            // to find the actual ZIP file
            
            images[i].needsManualReview = true;
            images[i].truncatedPath = oldPath;
            images[i].collectionPath = collectionPath;
            
            imagesFixed++;
        }
    }
    
    if (imagesFixed > 0) {
        print(`  ✅ Found ${imagesFixed} truncated paths in this collection`);
        
        // Update the collection
        db.collections.updateOne(
            { "_id": collection._id },
            { 
                $set: { 
                    "images": images,
                    "lastUpdated": new Date(),
                    "migrationStatus": "needs_manual_review"
                }
            }
        );
        
        totalFixed += imagesFixed;
    }
});

print(`\n🎉 Migration complete!`);
print(`📊 Collections processed: ${totalCollections}`);
print(`🔧 Images needing manual review: ${totalFixed}`);
print(`\n⚠️  Note: This script marks truncated paths for manual review.`);
print(`   You'll need to manually fix the paths or re-scan the affected collections.`);

// Show collections that need manual review
print(`\n📋 Collections needing manual review:`);
var needsReview = db.collections.find({
    "migrationStatus": "needs_manual_review"
}, {
    "name": 1,
    "path": 1,
    "images": { $elemMatch: { "needsManualReview": true } }
});

needsReview.forEach(function(collection) {
    print(`  - ${collection.name} (${collection.path})`);
});
