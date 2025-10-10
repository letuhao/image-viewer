// Check for duplicate system settings in MongoDB
db = db.getSiblingDB('image_viewer');

print('\n=== SYSTEM SETTINGS CHECK ===\n');

// Get all settings
var allSettings = db.system_settings.find({}, {
    settingKey: 1,
    settingValue: 1,
    category: 1,
    settingType: 1,
    _id: 1
}).sort({ settingKey: 1 }).toArray();

print('Total settings count: ' + allSettings.length);
print('\n--- All Settings ---');
allSettings.forEach(function(setting) {
    print(setting.settingKey + ' = ' + setting.settingValue + ' (Type: ' + setting.settingType + ', Category: ' + setting.category + ')');
});

// Check for duplicates
print('\n--- Checking for Duplicates ---');
var settingKeys = {};
var duplicates = [];

allSettings.forEach(function(setting) {
    if (settingKeys[setting.settingKey]) {
        duplicates.push(setting.settingKey);
        print('❌ DUPLICATE: ' + setting.settingKey);
        print('   ID 1: ' + settingKeys[setting.settingKey]);
        print('   ID 2: ' + setting._id);
    } else {
        settingKeys[setting.settingKey] = setting._id;
    }
});

if (duplicates.length === 0) {
    print('✅ No duplicates found');
} else {
    print('\n⚠️ Found ' + duplicates.length + ' duplicate settings');
}

// Show image processing settings specifically
print('\n--- Image Processing Settings ---');
db.system_settings.find({ category: 'ImageProcessing' }, {
    settingKey: 1,
    settingValue: 1,
    settingType: 1
}).sort({ settingKey: 1 }).forEach(function(setting) {
    print(setting.settingKey + ' = ' + setting.settingValue + ' (' + setting.settingType + ')');
});

