module.exports = {
  apps: [{
    name: 'image-viewer',
    script: 'server/index.js',
    instances: 1,
    exec_mode: 'fork',
    env: {
      NODE_ENV: 'production',
      PORT: 8081
    }
  }]
};