const fs = require('fs-extra');
const path = require('path');

/**
 * Utility class to handle long file paths on Windows
 * Windows has a 260 character limit for file paths
 */
class LongPathHandler {
  constructor() {
    this.maxPathLength = 250; // Leave some buffer for safety
    this.maxFilenameLength = 200; // Maximum filename length
  }

  /**
   * Validate if a path is too long for Windows
   * @param {string} filePath - The file path to validate
   * @returns {boolean} - True if path is too long
   */
  isPathTooLong(filePath) {
    return filePath.length > this.maxPathLength;
  }

  /**
   * Get a safe shortened path by truncating filename
   * @param {string} filePath - The original file path
   * @returns {string} - Shortened safe path
   */
  getSafePath(filePath) {
    if (!this.isPathTooLong(filePath)) {
      return filePath;
    }

    const dir = path.dirname(filePath);
    const ext = path.extname(filePath);
    const basename = path.basename(filePath, ext);
    
    // Calculate available space for filename
    const dirLength = dir.length;
    const extLength = ext.length;
    const availableLength = this.maxPathLength - dirLength - extLength - 2; // -2 for path separator
    
    if (availableLength <= 0) {
      throw new Error(`Path too long even after truncation: ${filePath.length} chars`);
    }

    // Truncate basename if needed
    let safeBasename = basename;
    if (basename.length > availableLength) {
      // Keep some of the original name for identification
      const keepLength = Math.max(10, availableLength - 10);
      const hash = this.generateShortHash(basename);
      safeBasename = basename.substring(0, keepLength) + '_' + hash.substring(0, 8);
    }

    return path.join(dir, safeBasename + ext);
  }

  /**
   * Generate a short hash for filename uniqueness
   * @param {string} input - Input string to hash
   * @returns {string} - Short hash
   */
  generateShortHash(input) {
    let hash = 0;
    for (let i = 0; i < input.length; i++) {
      const char = input.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32bit integer
    }
    return Math.abs(hash).toString(36);
  }

  /**
   * Ensure directory exists with path length validation
   * @param {string} dirPath - Directory path
   * @returns {Promise<string>} - Safe directory path
   */
  async ensureDirSafe(dirPath) {
    const safePath = this.getSafePath(dirPath);
    await fs.ensureDir(safePath);
    return safePath;
  }

  /**
   * Read file with path length validation
   * @param {string} filePath - File path
   * @returns {Promise<Buffer>} - File content
   */
  async readFileSafe(filePath) {
    if (this.isPathTooLong(filePath)) {
      throw new Error(`File path too long: ${filePath.length} chars (Windows limit: 260)`);
    }
    return await fs.readFile(filePath);
  }

  /**
   * Write file with path length validation
   * @param {string} filePath - File path
   * @param {Buffer|string} data - Data to write
   * @returns {Promise<void>}
   */
  async writeFileSafe(filePath, data) {
    const safePath = this.getSafePath(filePath);
    await fs.ensureDir(path.dirname(safePath));
    await fs.writeFile(safePath, data);
    return safePath;
  }

  /**
   * Check if path exists with length validation
   * @param {string} filePath - File path
   * @returns {Promise<boolean>} - True if exists
   */
  async pathExistsSafe(filePath) {
    if (this.isPathTooLong(filePath)) {
      return false;
    }
    return await fs.pathExists(filePath);
  }

  /**
   * Get file stats with path length validation
   * @param {string} filePath - File path
   * @returns {Promise<fs.Stats>} - File stats
   */
  async statSafe(filePath) {
    if (this.isPathTooLong(filePath)) {
      throw new Error(`File path too long: ${filePath.length} chars (Windows limit: 260)`);
    }
    return await fs.stat(filePath);
  }

  /**
   * Remove file/directory with path length validation
   * @param {string} filePath - File path
   * @returns {Promise<void>}
   */
  async removeSafe(filePath) {
    if (this.isPathTooLong(filePath)) {
      // Try to remove with shortened path
      const safePath = this.getSafePath(filePath);
      if (await fs.pathExists(safePath)) {
        await fs.remove(safePath);
      }
      return;
    }
    await fs.remove(filePath);
  }

  /**
   * Read directory with path length validation
   * @param {string} dirPath - Directory path
   * @param {Object} options - Options for readdir
   * @returns {Promise<string[]>} - Directory contents
   */
  async readDirSafe(dirPath, options = {}) {
    if (this.isPathTooLong(dirPath)) {
      throw new Error(`Directory path too long: ${dirPath.length} chars (Windows limit: 260)`);
    }
    return await fs.readdir(dirPath, options);
  }

  /**
   * Move file with path length validation
   * @param {string} src - Source path
   * @param {string} dest - Destination path
   * @returns {Promise<void>}
   */
  async moveSafe(src, dest) {
    const safeSrc = this.getSafePath(src);
    const safeDest = this.getSafePath(dest);
    
    await fs.ensureDir(path.dirname(safeDest));
    await fs.move(safeSrc, safeDest);
    return safeDest;
  }

  /**
   * Copy file with path length validation
   * @param {string} src - Source path
   * @param {string} dest - Destination path
   * @returns {Promise<void>}
   */
  async copySafe(src, dest) {
    const safeSrc = this.getSafePath(src);
    const safeDest = this.getSafePath(dest);
    
    await fs.ensureDir(path.dirname(safeDest));
    await fs.copy(safeSrc, safeDest);
    return safeDest;
  }

  /**
   * Join paths and validate total length
   * @param {...string} paths - Path segments
   * @returns {string} - Joined and validated path
   */
  joinSafe(...paths) {
    const joinedPath = path.join(...paths);
    return this.getSafePath(joinedPath);
  }

  /**
   * Get path info for debugging
   * @param {string} filePath - File path
   * @returns {Object} - Path information
   */
  getPathInfo(filePath) {
    return {
      originalPath: filePath,
      originalLength: filePath.length,
      isTooLong: this.isPathTooLong(filePath),
      safePath: this.getSafePath(filePath),
      safeLength: this.getSafePath(filePath).length,
      directory: path.dirname(filePath),
      filename: path.basename(filePath),
      extension: path.extname(filePath)
    };
  }
}

module.exports = new LongPathHandler();
