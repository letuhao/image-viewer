const fs = require('fs-extra');
const path = require('path');

class Logger {
  constructor(moduleName) {
    this.moduleName = moduleName;
    this.logDir = path.join(__dirname, '../../logs');
    this.logFile = path.join(this.logDir, `${moduleName}.log`);
    
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

  async writeToFile(logLine) {
    try {
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
