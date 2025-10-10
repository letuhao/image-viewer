// MongoDB Script to Create Admin User
// Run with: mongosh image_viewer scripts/create-admin-user.js

// Switch to image_viewer database
db = db.getSiblingDB('image_viewer');

// Check if admin user already exists
const existingAdmin = db.users.findOne({ username: 'admin' });

if (existingAdmin) {
    print('❌ Admin user already exists!');
    print(`   User ID: ${existingAdmin._id}`);
    print(`   Username: ${existingAdmin.username}`);
    print(`   Email: ${existingAdmin.email}`);
    print(`   Role: ${existingAdmin.role}`);
    print(`   Is Active: ${existingAdmin.isActive}`);
    exit(0);
}

// BCrypt password hash for "Admin@123456"
// This hash is generated using BCrypt with work factor 12
// Generated with: BCrypt.Net.BCrypt.HashPassword("Admin@123456", 12)
const passwordHash = "$2a$12$wdDsmLXWM2vqrneBM7PaFOyGADQVZQ.ae9WjExvOl.FmlAU1vaJJO";

// Create admin user ID first
const adminUserId = new ObjectId();

// Create admin user
const adminUser = {
    _id: adminUserId,
    username: 'admin',
    email: 'admin@imageviewer.local',
    passwordHash: passwordHash,
    isActive: true,
    isEmailVerified: true,
    profile: {
        firstName: 'System',
        lastName: 'Administrator',
        displayName: 'Admin',
        avatar: '',
        bio: 'System Administrator Account',
        location: '',
        website: '',
        birthDate: null,
        gender: '',
        language: 'en',
        timezone: 'UTC'
    },
    settings: {
        displayMode: 'grid',
        itemsPerPage: 20,
        theme: 'dark',
        language: 'en',
        timezone: 'UTC',
        notifications: {
            email: true,
            push: true,
            sms: false,
            inApp: true
        },
        privacy: {
            profileVisibility: 'public',
            activityVisibility: 'public',
            dataSharing: false,
            analytics: true
        },
        performance: {
            imageQuality: 'high',
            videoQuality: 'medium',
            cacheSize: 1073741824, // 1GB
            autoOptimize: true
        }
    },
    security: {
        _id: new ObjectId(),
        userId: adminUserId,
        twoFactorEnabled: false,
        twoFactorSecret: null,
        backupCodes: [],
        loginAttempts: [],
        securityQuestions: [],
        trustedDevices: [],
        ipWhitelist: [],
        lastPasswordChange: new Date(),
        passwordHistory: [],
        riskScore: 0.0,
        lastRiskAssessment: null,
        securitySettings: {
            requireTwoFactor: false,
            sessionTimeoutMinutes: 60,
            maxLoginAttempts: 5,
            lockoutDurationMinutes: 15,
            requireIpWhitelist: false,
            allowRememberMe: true,
            requirePasswordChange: false,
            passwordChangeIntervalDays: 90
        },
        createdAt: new Date(),
        updatedAt: new Date(),
        isDeleted: false,
        createdBy: null,
        updatedBy: null,
        createdBySystem: null,
        updatedBySystem: null
    },
    statistics: {
        totalViews: 0,
        totalSearches: 0,
        totalDownloads: 0,
        totalUploads: 0,
        totalCollections: 0,
        totalLibraries: 0,
        totalTags: 0,
        totalComments: 0,
        totalLikes: 0,
        totalShares: 0,
        totalFollowers: 0,
        totalFollowing: 0,
        totalPoints: 0,
        totalAchievements: 0,
        lastActivity: null,
        joinDate: new Date(),
        totalUsers: 0,
        activeUsers: 0,
        verifiedUsers: 0,
        newUsersThisMonth: 0,
        newUsersThisWeek: 0,
        newUsersToday: 0
    },
    role: 'Admin',
    twoFactorEnabled: false,
    twoFactorSecret: null,
    backupCodes: [],
    failedLoginAttempts: 0,
    isLocked: false,
    lockedUntil: null,
    lastLoginAt: null,
    lastLoginIp: null,
    createdAt: new Date(),
    updatedAt: new Date(),
    isDeleted: false,
    createdBy: null,
    updatedBy: null,
    createdBySystem: 'InitScript',
    updatedBySystem: 'InitScript'
};

// Insert admin user
const result = db.users.insertOne(adminUser);

if (result.acknowledged) {
    print('✅ Admin user created successfully!');
    print('');
    print('📋 Admin Credentials:');
    print('   Username: admin');
    print('   Password: Admin@123456');
    print('   Email: admin@imageviewer.local');
    print('   Role: Admin');
    print('');
    print(`   User ID: ${adminUser._id}`);
    print('');
    print('🔐 You can now login with these credentials!');
} else {
    print('❌ Failed to create admin user');
    printjson(result);
}

