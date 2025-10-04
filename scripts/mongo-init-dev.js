// MongoDB initialization script for development
db = db.getSiblingDB('image_viewer_dev');

// Create application user for development
db.createUser({
  user: 'imageviewer_dev',
  pwd: 'imageviewer_dev_password',
  roles: [
    {
      role: 'readWrite',
      db: 'image_viewer_dev'
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

// Insert some test data for development
db.users.insertOne({
  _id: ObjectId(),
  username: "testuser",
  email: "test@example.com",
  profile: {
    firstName: "Test",
    lastName: "User",
    avatarUrl: null
  },
  settings: {
    displayMode: "Grid",
    itemsPerPage: 20,
    theme: "light",
    language: "en"
  },
  security: {
    passwordHash: "hashed_password_here",
    twoFactorEnabled: false,
    lastLoginAt: null
  },
  statistics: {
    viewCount: 0,
    uploadCount: 0,
    downloadCount: 0
  },
  isActive: true,
  createdAt: new Date(),
  updatedAt: new Date()
});

print('MongoDB development initialization completed successfully');
