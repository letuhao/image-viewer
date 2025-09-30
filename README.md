# Image Viewer - Collection Manager

A powerful, web-based image viewer application designed for managing large collections of images from folders and ZIP files. Perfect for manga readers, photo collections, and digital art galleries.

## Features

### 🗂️ Collection Management
- **Multiple Collection Support**: Add collections from different folders and ZIP files across multiple disks
- **Smart Detection**: Automatically detects collection type (folder or ZIP) based on path
- **Rescan Functionality**: Update collections with new images without manual refresh
- **Collection Settings**: Customizable settings per collection

### 🖼️ Advanced Image Viewing
- **Manga-Style Viewer**: Full-screen viewing experience optimized for reading
- **Navigation Controls**: Keyboard shortcuts and on-screen controls for easy navigation
- **Auto-Play Slideshow**: Configurable slideshow with customizable intervals
- **Random Selection**: Jump to random images in your collection
- **Multiple View Modes**: Grid view for browsing, list view for detailed information

### ⚡ Performance & Caching
- **Smart Caching**: Automatic thumbnail generation and caching for fast loading
- **Virtual Scrolling**: Efficient rendering of large image collections
- **Lazy Loading**: Images load on-demand to reduce memory usage
- **Optimized Delivery**: Resized images based on viewing context

### 🔍 Search & Filter
- **Real-time Search**: Search images by filename across collections
- **Sorting Options**: Sort by filename, date, or file size
- **Filter by Type**: Filter collections by folder or ZIP type
- **Pagination**: Efficient handling of large collections

### ⌨️ Keyboard Shortcuts
- `←` / `A`: Previous image
- `→` / `D`: Next image
- `Space`: Play/pause slideshow
- `R`: Random image
- `F`: Toggle fullscreen
- `Esc`: Exit viewer or fullscreen
- `Home`: Return to collections

## Technology Stack

### Backend
- **Node.js + Express**: RESTful API server
- **MongoDB**: High-performance NoSQL database for metadata and caching
- **Sharp**: High-performance image processing and thumbnail generation
- **node-stream-zip**: Efficient ZIP file reading
- **Compression**: Gzip compression for faster transfers

### Frontend
- **React + TypeScript**: Modern, type-safe UI development
- **Vite**: Fast development and building
- **Tailwind CSS**: Utility-first styling with dark theme
- **Zustand**: Lightweight state management
- **React Router**: Client-side routing
- **React Window**: Virtual scrolling for performance

## Installation

### Prerequisites
- Node.js 18+ 
- npm or yarn

### Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd image-viewer
   ```

2. **Install dependencies**
   ```bash
   npm run install:all
   ```

3. **Setup MongoDB**
   ```bash
   npm run setup:mongodb
   ```
   
   This will:
   - Check if MongoDB is installed
   - Create necessary directories
   - Test the connection
   - Create environment configuration

4. **Start the development server**
   ```bash
   npm run dev
   ```

5. **Open your browser**
   Navigate to `http://localhost:4000`

### Production Build

1. **Build the application**
   ```bash
   npm run build
   ```

2. **Start the production server**
   ```bash
   npm start
   ```

### PM2 Production Deployment

For production deployment with PM2 process manager:

1. **Start with PM2**
   ```bash
   npm run pm2:start
   ```
   
   Or use the convenience scripts:
   - Windows: `start.bat`
   - Linux/Mac: `./start.sh`

2. **Check status**
   ```bash
   npm run pm2:status
   ```

3. **View logs**
   ```bash
   npm run pm2:logs
   ```

4. **Stop the application**
   ```bash
   npm run pm2:stop
   ```

### Deployment Scripts (Windows)

For easy deployment and updates:

1. **Full Deployment** (`deploy.bat`)
   - Stops PM2 processes
   - Installs all dependencies
   - Builds frontend
   - Starts server with PM2
   - Performs health checks
   
   ```bash
   deploy.bat
   ```

2. **Quick Deployment** (`quick-deploy.bat`)
   - Stops PM2 processes
   - Builds frontend only
   - Starts server with PM2
   - Skips dependency installation (faster)
   
   ```bash
   quick-deploy.bat
   ```

3. **Development Deployment** (`dev-deploy.bat`)
   - Stops PM2 processes
   - Installs dependencies
   - Builds frontend
   - Starts server in development mode (no PM2)
   
   ```bash
   dev-deploy.bat
   ```

## Usage

### Adding Collections

1. Click "Add Collection" in the main interface
2. Enter a name for your collection
3. Provide the path to either:
   - A folder containing images
   - A compressed file (supports .zip, .cbz, .cbr, .7z, .rar, .tar, .tar.gz, .tar.bz2)
4. The application will automatically scan and index all supported image formats

### Supported Compressed Formats

- **ZIP**: `.zip`, `.cbz` (Comic Book ZIP)
- **7-Zip**: `.7z`
- **RAR**: `.rar`, `.cbr` (Comic Book RAR)
- **TAR**: `.tar`, `.tar.gz`, `.tar.bz2`

**Note**: RAR and TAR extraction requires additional system tools and may have limited support. For best compatibility, consider using ZIP or 7Z formats.

### Supported Image Formats
- JPEG (.jpg, .jpeg)
- PNG (.png)
- GIF (.gif)
- WebP (.webp)
- BMP (.bmp)
- TIFF (.tiff)
- SVG (.svg)

### Viewing Images

1. **Browse Mode**: Click on any collection to view images in grid or list mode
2. **Viewer Mode**: Click "Open Viewer" or click on any image for full-screen viewing
3. **Navigation**: Use keyboard shortcuts or on-screen controls to navigate
4. **Slideshow**: Press spacebar or click play to start auto-advancing slideshow

### Performance Tips

- **Large Collections**: The app handles collections with thousands of images efficiently
- **Caching**: Thumbnails are generated automatically and cached for fast loading
- **Memory Management**: Images are loaded on-demand to minimize memory usage
- **Network Optimization**: Images are compressed and resized based on viewing context

## Configuration

### Environment Variables

Create a `.env` file in the root directory:

```env
PORT=8081
NODE_ENV=production
CACHE_DIR=./server/cache
TEMP_DIR=./server/temp
```

### Cache Management

- **Thumbnail Cache**: Stored in `server/cache/thumbnails/`
- **Database Cache**: SQLite database with automatic cleanup of expired entries
- **Clear Cache**: Use the cache management API or restart the server

## API Reference

### Collections
- `GET /api/collections` - List all collections
- `POST /api/collections` - Add new collection
- `GET /api/collections/:id` - Get collection details
- `PUT /api/collections/:id` - Update collection
- `DELETE /api/collections/:id` - Delete collection
- `POST /api/collections/:id/scan` - Rescan collection

### Images
- `GET /api/collections/:id/images` - List collection images
- `GET /api/images/:collectionId/:imageId/file` - Get image file
- `GET /api/images/:collectionId/:imageId/thumbnail` - Get thumbnail
- `GET /api/images/:collectionId/:imageId/navigate` - Navigate to next/previous
- `GET /api/images/:collectionId/random` - Get random image
- `GET /api/images/:collectionId/search` - Search images

### Cache
- `DELETE /api/cache` - Clear all cache
- `GET /api/cache/stats` - Get cache statistics

## Development

### Project Structure
```
image-viewer/
├── server/                 # Backend API server
│   ├── routes/            # API route handlers
│   ├── cache/             # Generated thumbnails
│   └── database.sqlite    # SQLite database
├── client/                # React frontend
│   ├── src/
│   │   ├── components/    # React components
│   │   ├── pages/         # Page components
│   │   ├── store/         # State management
│   │   └── services/      # API services
│   └── public/            # Static assets
└── package.json           # Dependencies and scripts
```

### Development Scripts
- `npm run dev` - Start development server (both frontend and backend)
- `npm run server:dev` - Start backend only
- `npm run client:dev` - Start frontend only
- `npm run build` - Build for production
- `npm start` - Start production server

### PM2 Production Scripts
- `npm run pm2:start` - Start with PM2 process manager
- `npm run pm2:stop` - Stop PM2 process
- `npm run pm2:restart` - Restart PM2 process
- `npm run pm2:logs` - View PM2 logs
- `npm run pm2:status` - Check PM2 status

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

MIT License - see LICENSE file for details.

## Support

For issues, feature requests, or questions:
1. Check the existing issues
2. Create a new issue with detailed description
3. Include system information and error logs if applicable

---

**Note**: This application is designed to handle large image collections efficiently. For optimal performance with very large collections (10,000+ images), consider running on a system with adequate RAM and SSD storage.