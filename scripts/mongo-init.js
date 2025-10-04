// MongoDB initialization script for production
db = db.getSiblingDB('image_viewer');

// Create application user
db.createUser({
  user: 'imageviewer_user',
  pwd: 'imageviewer_password',
  roles: [
    {
      role: 'readWrite',
      db: 'image_viewer'
    }
  ]
});

// Create collections with proper indexes
db.createCollection('users');
db.createCollection('libraries');
db.createCollection('collections');
db.createCollection('mediaitems');
db.createCollection('notifications');
db.createCollection('userpreferences');
db.createCollection('securityalerts');
db.createCollection('performancemetrics');

// Create indexes for better performance
db.users.createIndex({ "username": 1 }, { unique: true });
db.users.createIndex({ "email": 1 }, { unique: true });
db.users.createIndex({ "createdAt": 1 });

db.libraries.createIndex({ "name": 1 });
db.libraries.createIndex({ "path": 1 }, { unique: true });
db.libraries.createIndex({ "createdAt": 1 });

db.collections.createIndex({ "libraryId": 1 });
db.collections.createIndex({ "name": 1 });
db.collections.createIndex({ "createdAt": 1 });

db.mediaitems.createIndex({ "collectionId": 1 });
db.mediaitems.createIndex({ "filename": 1 });
db.mediaitems.createIndex({ "createdAt": 1 });

db.notifications.createIndex({ "userId": 1 });
db.notifications.createIndex({ "createdAt": 1 });
db.notifications.createIndex({ "status": 1 });

db.userpreferences.createIndex({ "userId": 1 }, { unique: true });

db.securityalerts.createIndex({ "userId": 1 });
db.securityalerts.createIndex({ "createdAt": 1 });
db.securityalerts.createIndex({ "type": 1 });

db.performancemetrics.createIndex({ "timestamp": 1 });

print('MongoDB initialization completed successfully');
