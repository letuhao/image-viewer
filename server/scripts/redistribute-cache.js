const { MongoClient, ObjectId } = require('mongodb');

async function redistributeCache() {
  const client = new MongoClient('mongodb://localhost:27017');
  await client.connect();
  const db = client.db('image_viewer');
  
  const cacheFolders = db.collection('cache_folders');
  const collectionCacheBindings = db.collection('collection_cache_bindings');
  
  console.log('🔄 Starting cache redistribution...');
  
  // Get all active cache folders sorted by size (empty first)
  const folders = await cacheFolders.find({ is_active: true })
    .sort({ current_size: 1, priority: -1 })
    .toArray();
  
  console.log(`📁 Found ${folders.length} active cache folders:`);
  folders.forEach(folder => {
    console.log(`  - ${folder.name}: ${folder.current_size} bytes`);
  });
  
  // Get all collections
  const collections = await db.collection('collections').find({}).toArray();
  console.log(`📚 Found ${collections.length} collections`);
  
  // Clear all existing bindings
  await collectionCacheBindings.deleteMany({});
  console.log('🗑️ Cleared all existing cache bindings');
  
  // Reset cache folder usage
  await cacheFolders.updateMany({}, { 
    $set: { 
      current_size: 0, 
      file_count: 0,
      updated_at: new Date()
    } 
  });
  console.log('🔄 Reset cache folder usage counters');
  
  // Redistribute collections
  let folderIndex = 0;
  for (const collection of collections) {
    const selectedFolder = folders[folderIndex % folders.length];
    
    await collectionCacheBindings.insertOne({
      collection_id: collection._id,
      cache_folder_id: selectedFolder._id,
      created_at: new Date(),
      updated_at: new Date()
    });
    
    // Update folder usage (estimate based on collection settings)
    const estimatedSize = collection.settings?.total_size || 0;
    await cacheFolders.updateOne(
      { _id: selectedFolder._id },
      { 
        $inc: { 
          current_size: estimatedSize,
          file_count: collection.settings?.total_images || 0
        },
        $set: { updated_at: new Date() }
      }
    );
    
    folderIndex++;
    
    if (collection._id % 10 === 0) {
      console.log(`✅ Processed ${folderIndex}/${collections.length} collections`);
    }
  }
  
  console.log('🎉 Cache redistribution completed!');
  
  // Show final stats
  const finalFolders = await cacheFolders.find({ is_active: true })
    .sort({ current_size: 1 })
    .toArray();
  
  console.log('\n📊 Final cache distribution:');
  finalFolders.forEach(folder => {
    const sizeMB = (folder.current_size / 1024 / 1024).toFixed(2);
    console.log(`  - ${folder.name}: ${sizeMB} MB (${folder.file_count} files)`);
  });
  
  await client.close();
}

redistributeCache().catch(console.error);
