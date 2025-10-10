// Remove old PascalCase cache/thumbnail settings that are not being used
// The new dot-notation settings (cache.default.format, etc.) are the correct ones

db = db.getSiblingDB('image_viewer');

print('\n=== CLEANUP OLD CACHE SETTINGS ===\n');

// List of old PascalCase settings to remove
var oldSettings = [
    'Cache.DefaultFormat',
    'Cache.DefaultQuality',
    'Cache.DefaultWidth',
    'Cache.DefaultHeight',
    'Cache.PreserveOriginal',
    'Cache.QualityPresets',
    'Thumbnail.DefaultSize',
    'Thumbnail.Format',
    'Thumbnail.Quality',
    'BulkAdd.AutoScan',
    'BulkAdd.DefaultFormat',
    'BulkAdd.DefaultQuality',
    'BulkAdd.GenerateCache',
    'BulkAdd.GenerateThumbnails'
];

print('Settings to remove:');
oldSettings.forEach(function(key) {
    print('  - ' + key);
});

print('\n--- Deleting old settings ---');

var result = db.system_settings.deleteMany({
    settingKey: { $in: oldSettings }
});

print('✅ Deleted ' + result.deletedCount + ' old settings\n');

// Verify remaining settings
print('--- Remaining settings ---');
db.system_settings.find({}, {
    settingKey: 1,
    settingValue: 1,
    category: 1
}).sort({ settingKey: 1 }).forEach(function(setting) {
    print(setting.settingKey + ' = ' + setting.settingValue + ' (Category: ' + setting.category + ')');
});

print('\n✅ Cleanup complete!');

