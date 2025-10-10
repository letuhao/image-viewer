// MongoDB script to add cache format settings to system_settings collection
// Run with: mongosh mongodb://localhost:27017/image_viewer scripts/add-format-settings.js

use('image_viewer');

// Add cache format settings
const formatSettings = [
    {
        _id: ObjectId(),
        settingKey: 'cache.default.format',
        settingValue: 'jpeg',
        settingType: 'String',
        category: 'ImageProcessing',
        description: 'Default image format for cache generation (jpeg, png, webp)',
        isEditable: true,
        createdAt: new Date(),
        updatedAt: new Date(),
        isDeleted: false
    },
    {
        _id: ObjectId(),
        settingKey: 'cache.default.quality',
        settingValue: '85',
        settingType: 'Integer',
        category: 'ImageProcessing',
        description: 'Default quality for cache generation (0-100)',
        isEditable: true,
        createdAt: new Date(),
        updatedAt: new Date(),
        isDeleted: false
    },
    {
        _id: ObjectId(),
        settingKey: 'thumbnail.default.format',
        settingValue: 'jpeg',
        settingType: 'String',
        category: 'ImageProcessing',
        description: 'Default image format for thumbnail generation (jpeg, png, webp)',
        isEditable: true,
        createdAt: new Date(),
        updatedAt: new Date(),
        isDeleted: false
    },
    {
        _id: ObjectId(),
        settingKey: 'thumbnail.default.quality',
        settingValue: '90',
        settingType: 'Integer',
        category: 'ImageProcessing',
        description: 'Default quality for thumbnail generation (0-100)',
        isEditable: true,
        createdAt: new Date(),
        updatedAt: new Date(),
        isDeleted: false
    },
    {
        _id: ObjectId(),
        settingKey: 'thumbnail.default.size',
        settingValue: '300',
        settingType: 'Integer',
        category: 'ImageProcessing',
        description: 'Default thumbnail size in pixels',
        isEditable: true,
        createdAt: new Date(),
        updatedAt: new Date(),
        isDeleted: false
    }
];

// Insert settings
try {
    // Check if settings already exist
    const existingSettings = db.system_settings.find({
        settingKey: { $in: formatSettings.map(s => s.settingKey) }
    }).toArray();

    if (existingSettings.length > 0) {
        print('⚠️  Some settings already exist. Updating...');
        
        formatSettings.forEach(setting => {
            const result = db.system_settings.updateOne(
                { settingKey: setting.settingKey },
                { 
                    $set: {
                        settingValue: setting.settingValue,
                        settingType: setting.settingType,
                        category: setting.category,
                        description: setting.description,
                        isEditable: setting.isEditable,
                        updatedAt: new Date()
                    },
                    $setOnInsert: {
                        _id: setting._id,
                        createdAt: setting.createdAt,
                        isDeleted: false
                    }
                },
                { upsert: true }
            );
            
            if (result.upsertedCount > 0) {
                print(`✅ Inserted: ${setting.settingKey}`);
            } else if (result.modifiedCount > 0) {
                print(`✅ Updated: ${setting.settingKey}`);
            } else {
                print(`ℹ️  No change: ${setting.settingKey}`);
            }
        });
    } else {
        const result = db.system_settings.insertMany(formatSettings);
        print(`✅ Successfully inserted ${result.insertedIds.length} format settings!`);
    }
    
    print('\n📋 Current Format Settings:');
    db.system_settings.find({ 
        category: 'ImageProcessing',
        settingKey: { $regex: /(cache|thumbnail)\.(default)\.(format|quality|size)/ }
    }).forEach(setting => {
        print(`   • ${setting.settingKey}: ${setting.settingValue}`);
    });
    
    print('\n✅ Format settings configuration complete!');
} catch (error) {
    print(`❌ Error: ${error.message}`);
}

