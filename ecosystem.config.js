module.exports = {
  apps: [
    {
      name: 'image-viewer',
      script: 'server/index.js',
      cwd: './',
      instances: 1,
      autorestart: true,
      watch: false,
      max_memory_restart: '1G',
      env: {
        NODE_ENV: 'production',
        PORT: 8081,
        CACHE_DIR: './server/cache',
        TEMP_DIR: './server/temp'
      },
      env_development: {
        NODE_ENV: 'development',
        PORT: 8081,
        CACHE_DIR: './server/cache',
        TEMP_DIR: './server/temp'
      },
      error_file: './logs/err.log',
      out_file: './logs/out.log',
      log_file: './logs/combined.log',
      time: true,
      log_date_format: 'YYYY-MM-DD HH:mm:ss Z',
      merge_logs: true
    }
  ]
};
