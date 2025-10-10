// MongoDB script to remove old/deprecated system settings
// Run with: mongosh mongodb://localhost:27017/image_viewer scripts/cleanup-old-settings.js

use('image_viewer');

print('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
print('🧹 CLEANING UP OLD SYSTEM SETTINGS');
print('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n');

// List of old settings to remove (PascalCase versions)
const oldSettingKeys = [
    'Cache.DefaultFormat',
    'Cache.DefaultQuality',
    'Cache.DefaultWidth',
    'Cache.DefaultHeight',
    'Cache.PreserveOriginal',
    'Cache.QualityPresets',
    'Thumbnail.DefaultSize',
    'Thumbnail.Format',
    'Thumbnail.Quality',
    'BulkAdd.DefaultFormat',
    'BulkAdd.DefaultQuality',
    'BulkAdd.AutoScan',
    'BulkAdd.GenerateCache',
    'BulkAdd.GenerateThumbnails'
];

print('📋 OLD SETTINGS TO REMOVE (PascalCase):');
oldSettingKeys.forEach(key => print('   • ' + key));
print('');

// Check which ones exist
const existingOldSettings = db.system_settings.find({
    settingKey: { $in: oldSettingKeys }
}).toArray();

print(`📊 Found ${existingOldSettings.length} old settings in database:`);
existingOldSettings.forEach(s => {
    print(`   • ${s.settingKey} = ${s.settingValue}`);
});
print('');

if (existingOldSettings.length === 0) {
    print('✅ No old settings found. Database is clean!');
} else {
    // Ask for confirmation (in script we'll just do it)
    print('🗑️  Removing old settings...');
    
    const result = db.system_settings.deleteMany({
        settingKey: { $in: oldSettingKeys }
    });
    
    print(`✅ Removed ${result.deletedCount} old settings!`);
}

print('\n📋 CURRENT SETTINGS (dot notation - CORRECT):');
db.system_settings.find({
    settingKey: { $regex: /^[a-z]+\.[a-z]+\.(format|quality|size)$/ }
}).sort({ settingKey: 1 }).forEach(s => {
    print(`   ✅ ${s.settingKey} = ${s.settingValue}`);
});

print('\n✅ Cleanup complete!');
print('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');

