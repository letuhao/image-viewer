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
      PORT: 10001
    },
    error_file: './logs/err.log',
    out_file: './logs/out.log',
    log_file: './logs/combined.log',
    time: true
  }]
};