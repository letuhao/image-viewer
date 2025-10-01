const fs = require('fs-extra');
const path = require('path');

class Logger {
  constructor(moduleName) {
    this.moduleName = moduleName;
    this.logDir = path.join(__dirname, '../../logs');
    this.logFile = path.join(this.logDir, `${moduleName}.log`);
    
    // Log rotation configuration from environment variables
    this.maxFileSize = parseInt(process.env.LOG_MAX_FILE_SIZE || '10485760'); // 10MB default
    this.maxFiles = parseInt(process.env.LOG_MAX_FILES || '10'); // 10 files default
    
    // Ensure log directory exists
    fs.ensureDirSync(this.logDir);
  }

  formatMessage(level, message, data = null) {
    const timestamp = new Date().toISOString();
    const logEntry = {
      timestamp,
      level,
      module: this.moduleName,
      message,
      data
    };
    
    const logLine = data 
      ? `[${timestamp}] [${level}] [${this.moduleName}] ${message}\nData: ${JSON.stringify(data, null, 2)}\n`
      : `[${timestamp}] [${level}] [${this.moduleName}] ${message}\n`;
    
    return { logEntry, logLine };
  }

  async checkAndRotateLog() {
    try {
      // Check if log file exists and get its size
      if (await fs.pathExists(this.logFile)) {
        const stats = await fs.stat(this.logFile);
        
        // If file size exceeds maxFileSize, rotate
        if (stats.size >= this.maxFileSize) {
          await this.rotateLogFile();
        }
      }
    } catch (error) {
      console.error(`Failed to check/rotate log file ${this.logFile}:`, error);
    }
  }

  async rotateLogFile() {
    try {
      // Remove the oldest log file if we have maxFiles
      const oldestFile = path.join(this.logDir, `${this.moduleName}.log.${this.maxFiles - 1}`);
      if (await fs.pathExists(oldestFile)) {
        await fs.remove(oldestFile);
      }

      // Shift existing log files (rename .log.1 to .log.2, etc.)
      for (let i = this.maxFiles - 1; i >= 1; i--) {
        const currentFile = path.join(this.logDir, `${this.moduleName}.log.${i}`);
        const nextFile = path.join(this.logDir, `${this.moduleName}.log.${i + 1}`);
        
        // Delete the target file if it exists
        if (await fs.pathExists(nextFile)) {
          await fs.remove(nextFile);
        }
        
        // Move current file to next position
        if (await fs.pathExists(currentFile)) {
          await fs.move(currentFile, nextFile);
        }
      }

      // Move current log file to .log.1 (delete old .log.1 first)
      if (await fs.pathExists(this.logFile)) {
        const rotatedFile = path.join(this.logDir, `${this.moduleName}.log.1`);
        if (await fs.pathExists(rotatedFile)) {
          await fs.remove(rotatedFile);
        }
        await fs.move(this.logFile, rotatedFile);
      }

      // Don't use logger here to avoid infinite recursion
      console.log(`[LOGGER] Log rotated for ${this.moduleName}. New log file created.`);
    } catch (error) {
      // Don't use logger here to avoid infinite recursion
      console.error(`Failed to rotate log file ${this.logFile}:`, error);
    }
  }

  async writeToFile(logLine) {
    try {
      // Check and rotate log file if necessary before writing
      await this.checkAndRotateLog();
      
      await fs.appendFile(this.logFile, logLine);
    } catch (error) {
      console.error(`Failed to write to log file ${this.logFile}:`, error);
    }
  }

  info(message, data = null) {
    const { logEntry, logLine } = this.formatMessage('INFO', message, data);
    console.log(`[${logEntry.timestamp}] [INFO] [${this.moduleName}] ${message}`);
    this.writeToFile(logLine);
  }

  debug(message, data = null) {
    const { logEntry, logLine } = this.formatMessage('DEBUG', message, data);
    console.log(`[${logEntry.timestamp}] [DEBUG] [${this.moduleName}] ${message}`);
    this.writeToFile(logLine);
  }

  warn(message, data = null) {
    const { logEntry, logLine } = this.formatMessage('WARN', message, data);
    console.warn(`[${logEntry.timestamp}] [WARN] [${this.moduleName}] ${message}`);
    this.writeToFile(logLine);
  }

  error(message, data = null) {
    const { logEntry, logLine } = this.formatMessage('ERROR', message, data);
    console.error(`[${logEntry.timestamp}] [ERROR] [${this.moduleName}] ${message}`);
    this.writeToFile(logLine);
  }

  // Special method for flow tracking
  flow(step, data = null) {
    const { logEntry, logLine } = this.formatMessage('FLOW', `FLOW: ${step}`, data);
    console.log(`[${logEntry.timestamp}] [FLOW] [${this.moduleName}] ${step}`);
    this.writeToFile(logLine);
  }

  // Special method for performance tracking
  perf(operation, startTime, data = null) {
    const duration = Date.now() - startTime;
    const { logEntry, logLine } = this.formatMessage('PERF', `PERF: ${operation} took ${duration}ms`, data);
    console.log(`[${logEntry.timestamp}] [PERF] [${this.moduleName}] ${operation} took ${duration}ms`);
    this.writeToFile(logLine);
  }
}

module.exports = Logger;
