module.exports = {
  apps: [{
    name: 'image-viewer',
    script: 'server/index.js',
    cwd: 'D:\\Works\\source\\image-viewer',
    instances: 1,
    autorestart: true,
    watch: false,
    max_memory_restart: '1G',
    env: {
      NODE_ENV: 'production',
      PORT: 10001,
      LOG_MAX_FILE_SIZE: '10485760',
      LOG_MAX_FILES: '10',
      ENABLE_PARALLEL_CACHE_PROCESSING: 'true',
      MAX_CONCURRENT_CACHE_PROCESSES: '1'
    },
    error_file: './logs/err.log',
    out_file: './logs/out.log',
    log_file: './logs/combined.log',
    time: true
  }]
};