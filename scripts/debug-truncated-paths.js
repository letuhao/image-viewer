// Debug script to find collections with truncated ZIP paths
// This will help identify which collection is causing the resume logic issue

use image_viewer;

print("🔍 Searching for collections with truncated ZIP paths...");

// Find collections with paths that might be truncated
var collections = db.collections.find({
    "path": { $regex: /\[Mr\. Teardrop\].*Mash Kyrielight/ }
});

print(`\n📊 Found ${collections.count()} collections matching the pattern:`);

collections.forEach(function(collection) {
    print(`\n📁 Collection: ${collection.name}`);
    print(`   ID: ${collection._id}`);
    print(`   Path: ${collection.path}`);
    print(`   Images count: ${collection.images ? collection.images.length : 0}`);
    print(`   Thumbnails count: ${collection.thumbnails ? collection.thumbnails.length : 0}`);
    print(`   Cache count: ${collection.cacheImages ? collection.cacheImages.length : 0}`);
    
    // Check if path looks truncated
    if (collection.path && !collection.path.includes("#3 (FateGrand Order)")) {
        print(`   ⚠️  POTENTIALLY TRUNCATED PATH!`);
    }
    
    // Check if there are images with truncated paths
    if (collection.images && collection.images.length > 0) {
        print(`   📸 Images:`);
        collection.images.forEach(function(image, index) {
            if (index < 3) { // Show first 3 images
                print(`      ${index + 1}. ${image.filename || 'No filename'} - ${image.legacyRelativePath || 'No path'}`);
            }
        });
        if (collection.images.length > 3) {
            print(`      ... and ${collection.images.length - 3} more`);
        }
    }
});

// Also search for any collections with "Mash Kyrielight" in the name
print(`\n🔍 Searching for any collections with "Mash Kyrielight" in name...`);

var nameCollections = db.collections.find({
    "name": { $regex: /Mash Kyrielight/i }
});

print(`\n📊 Found ${nameCollections.count()} collections with "Mash Kyrielight" in name:`);

nameCollections.forEach(function(collection) {
    print(`\n📁 Collection: ${collection.name}`);
    print(`   ID: ${collection._id}`);
    print(`   Path: ${collection.path}`);
    print(`   Images count: ${collection.images ? collection.images.length : 0}`);
    
    // Check if this looks like the problematic collection
    if (collection.path && collection.path.includes("Mash Kyrielight") && !collection.path.includes("#3")) {
        print(`   🚨 THIS MIGHT BE THE PROBLEMATIC COLLECTION!`);
        print(`   🚨 Path is truncated: ${collection.path}`);
    }
});

print(`\n✅ Debug complete!`);
