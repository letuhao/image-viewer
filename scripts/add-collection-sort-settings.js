// MongoDB script to add collection sorting system settings
// Run: mongosh image_viewer < add-collection-sort-settings.js

db = db.getSiblingDB('image_viewer');

print('Adding collection sorting system settings...');

const settings = [
  {
    settingKey: 'collection.sort.default.field',
    settingValue: 'updatedAt',
    settingType: 'String',
    category: 'Collections',
    description: 'Default field to sort collections (createdAt, updatedAt, name, imageCount, totalSize)',
    isEncrypted: false,
    isSensitive: false,
    isReadOnly: false,
    defaultValue: 'updatedAt',
    validationRules: {},
    lastModifiedBy: null,
    source: 'System',
    version: 1,
    changeHistory: [],
    environment: 'All',
    isActive: true,
    isEditable: true,
    createdAt: new Date(),
    updatedAt: new Date(),
    isDeleted: false,
    createdBy: null,
    updatedBy: null,
    createdBySystem: 'System',
    updatedBySystem: 'System'
  },
  {
    settingKey: 'collection.sort.default.direction',
    settingValue: 'desc',
    settingType: 'String',
    category: 'Collections',
    description: 'Default sort direction for collections (asc, desc)',
    isEncrypted: false,
    isSensitive: false,
    isReadOnly: false,
    defaultValue: 'desc',
    validationRules: {},
    lastModifiedBy: null,
    source: 'System',
    version: 1,
    changeHistory: [],
    environment: 'All',
    isActive: true,
    isEditable: true,
    createdAt: new Date(),
    updatedAt: new Date(),
    isDeleted: false,
    createdBy: null,
    updatedBy: null,
    createdBySystem: 'System',
    updatedBySystem: 'System'
  }
];

settings.forEach(setting => {
  const existing = db.system_settings.findOne({ settingKey: setting.settingKey });
  if (existing) {
    print(`Setting ${setting.settingKey} already exists, skipping...`);
  } else {
    db.system_settings.insertOne(setting);
    print(`✅ Added setting: ${setting.settingKey}`);
  }
});

print('\n📊 Current collection sorting settings:');
db.system_settings.find({ 
  category: 'Collections' 
}).forEach(s => {
  print(`  ${s.settingKey}: ${s.settingValue}`);
});

print('\n✅ Collection sorting settings initialized!');

