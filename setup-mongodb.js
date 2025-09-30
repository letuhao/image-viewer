#!/usr/bin/env node

const fs = require('fs-extra');
const path = require('path');
const { exec } = require('child_process');
const { promisify } = require('util');

const execAsync = promisify(exec);

console.log('🚀 MongoDB Setup for Image Viewer');
console.log('===================================\n');

async function checkMongoDBInstallation() {
  try {
    const { stdout } = await execAsync('mongod --version');
    console.log('✅ MongoDB is installed:');
    console.log(stdout);
    return true;
  } catch (error) {
    console.log('❌ MongoDB is not installed or not in PATH');
    console.log('\nTo install MongoDB:');
    console.log('1. Download from: https://www.mongodb.com/try/download/community');
    console.log('2. Or use package manager:');
    console.log('   - Windows: choco install mongodb');
    console.log('   - macOS: brew install mongodb-community');
    console.log('   - Ubuntu: sudo apt install mongodb');
    return false;
  }
}

async function setupMongoDBDataDirectory() {
  const dataDir = path.join(__dirname, 'data', 'mongodb');
  
  try {
    await fs.ensureDir(dataDir);
    console.log(`✅ MongoDB data directory created: ${dataDir}`);
    return dataDir;
  } catch (error) {
    console.error('❌ Failed to create MongoDB data directory:', error);
    throw error;
  }
}

async function checkMongoDBRunning() {
  try {
    const { stdout } = await execAsync('mongosh --eval "db.runCommand({ping: 1})" --quiet');
    console.log('✅ MongoDB is running and accessible');
    return true;
  } catch (error) {
    console.log('❌ MongoDB is not running or not accessible');
    console.log('\nTo start MongoDB:');
    console.log('1. Start MongoDB service:');
    console.log('   - Windows: net start MongoDB');
    console.log('   - macOS: brew services start mongodb-community');
    console.log('   - Ubuntu: sudo systemctl start mongod');
    console.log('2. Or start manually: npm run mongodb:start');
    return false;
  }
}

async function createEnvironmentFile() {
  const envPath = path.join(__dirname, '.env');
  const envExamplePath = path.join(__dirname, 'env.example');
  
  if (await fs.pathExists(envPath)) {
    console.log('✅ .env file already exists');
    return;
  }
  
  if (await fs.pathExists(envExamplePath)) {
    await fs.copy(envExamplePath, envPath);
    console.log('✅ Created .env file from env.example');
  } else {
    const envContent = `# Server Configuration
PORT=8081
NODE_ENV=development

# Cache Configuration
CACHE_DIR=./server/cache
TEMP_DIR=./server/temp

# Database Configuration (MongoDB)
MONGODB_URL=mongodb://localhost:27017
MONGODB_DB_NAME=image_viewer

# Image Processing
MAX_THUMBNAIL_SIZE=300
IMAGE_QUALITY=80
MAX_IMAGE_SIZE=2048
`;
    await fs.writeFile(envPath, envContent);
    console.log('✅ Created .env file with default MongoDB configuration');
  }
}

async function testMongoDBConnection() {
  console.log('\n🧪 Testing MongoDB connection...');
  
  try {
    const MongoDBDatabase = require('./server/mongodb');
    const db = new MongoDBDatabase();
    
    await db.connect();
    console.log('✅ Successfully connected to MongoDB');
    
    // Test basic operations
    const testCollection = db.db.collection('test');
    await testCollection.insertOne({ test: 'connection', timestamp: new Date() });
    console.log('✅ Successfully wrote test data');
    
    const result = await testCollection.findOne({ test: 'connection' });
    console.log('✅ Successfully read test data');
    
    await testCollection.deleteOne({ test: 'connection' });
    console.log('✅ Successfully cleaned up test data');
    
    await db.close();
    console.log('✅ MongoDB connection test completed successfully');
    
    return true;
  } catch (error) {
    console.error('❌ MongoDB connection test failed:', error.message);
    return false;
  }
}

async function main() {
  try {
    console.log('1. Checking MongoDB installation...');
    const mongoInstalled = await checkMongoDBInstallation();
    
    if (!mongoInstalled) {
      console.log('\n⚠️  Please install MongoDB first, then run this setup again.');
      process.exit(1);
    }
    
    console.log('\n2. Setting up MongoDB data directory...');
    await setupMongoDBDataDirectory();
    
    console.log('\n3. Checking if MongoDB is running...');
    const mongoRunning = await checkMongoDBRunning();
    
    if (!mongoRunning) {
      console.log('\n⚠️  Please start MongoDB first, then run this setup again.');
      console.log('   You can start it with: npm run mongodb:start');
      process.exit(1);
    }
    
    console.log('\n4. Creating environment configuration...');
    await createEnvironmentFile();
    
    console.log('\n5. Testing MongoDB connection...');
    const connectionSuccess = await testMongoDBConnection();
    
    if (connectionSuccess) {
      console.log('\n🎉 MongoDB setup completed successfully!');
      console.log('\nNext steps:');
      console.log('1. Start the application: npm start');
      console.log('2. Or start with PM2: npm run pm2:start');
      console.log('3. Access the application at: http://localhost:8081');
    } else {
      console.log('\n❌ Setup failed. Please check your MongoDB configuration.');
      process.exit(1);
    }
    
  } catch (error) {
    console.error('\n❌ Setup failed:', error);
    process.exit(1);
  }
}

// Run setup if called directly
if (require.main === module) {
  main();
}

module.exports = { main };
