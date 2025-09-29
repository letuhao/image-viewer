#!/usr/bin/env node

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

console.log('🚀 Setting up Image Viewer...\n');

// Check if Node.js version is compatible
const nodeVersion = process.version;
const majorVersion = parseInt(nodeVersion.slice(1).split('.')[0]);

if (majorVersion < 18) {
  console.error('❌ Node.js 18 or higher is required. Current version:', nodeVersion);
  process.exit(1);
}

console.log('✅ Node.js version check passed:', nodeVersion);

// Install root dependencies
console.log('\n📦 Installing root dependencies...');
try {
  execSync('npm install', { stdio: 'inherit' });
  console.log('✅ Root dependencies installed');
} catch (error) {
  console.error('❌ Failed to install root dependencies');
  process.exit(1);
}

// Install client dependencies
console.log('\n📦 Installing client dependencies...');
try {
  process.chdir('client');
  execSync('npm install', { stdio: 'inherit' });
  process.chdir('..');
  console.log('✅ Client dependencies installed');
} catch (error) {
  console.error('❌ Failed to install client dependencies');
  process.exit(1);
}

// Create necessary directories
console.log('\n📁 Creating directories...');
const directories = [
  'server/cache',
  'server/cache/thumbnails',
  'server/temp'
];

directories.forEach(dir => {
  const dirPath = path.join(__dirname, dir);
  if (!fs.existsSync(dirPath)) {
    fs.mkdirSync(dirPath, { recursive: true });
    console.log(`✅ Created directory: ${dir}`);
  } else {
    console.log(`✅ Directory exists: ${dir}`);
  }
});

// Create .env file if it doesn't exist
console.log('\n⚙️  Setting up environment...');
const envPath = path.join(__dirname, '.env');
if (!fs.existsSync(envPath)) {
  const envContent = `PORT=3001
NODE_ENV=development
CACHE_DIR=./server/cache
TEMP_DIR=./server/temp
`;
  fs.writeFileSync(envPath, envContent);
  console.log('✅ Created .env file');
} else {
  console.log('✅ .env file already exists');
}

console.log('\n🎉 Setup complete!');
console.log('\n📋 Next steps:');
console.log('1. Run "npm run dev" to start the development server');
console.log('2. Open http://localhost:3000 in your browser');
console.log('3. Add your first collection using the "Add Collection" button');
console.log('\n💡 Tip: You can add collections from folders or ZIP files containing images');
console.log('\n📚 For more information, see the README.md file');

// Check if we should start the development server
const args = process.argv.slice(2);
if (args.includes('--start') || args.includes('-s')) {
  console.log('\n🚀 Starting development server...');
  try {
    execSync('npm run dev', { stdio: 'inherit' });
  } catch (error) {
    console.error('❌ Failed to start development server');
  }
}
