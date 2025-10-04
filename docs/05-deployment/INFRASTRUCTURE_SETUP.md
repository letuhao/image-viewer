# Infrastructure Setup Guide - ImageViewer Platform

## 📋 Tổng Quan

Document này mô tả chi tiết cách setup infrastructure cho ImageViewer Platform, bao gồm database, message queue, cache, và monitoring systems.

## 🎯 Infrastructure Requirements

### **Minimum Requirements**
- **CPU**: 8 cores (4 cores per environment)
- **Memory**: 16GB RAM (8GB per environment)
- **Storage**: 100GB SSD (50GB per environment)
- **Network**: 1Gbps bandwidth
- **OS**: Ubuntu 20.04 LTS hoặc CentOS 8

### **Recommended Requirements**
- **CPU**: 16 cores (8 cores per environment)
- **Memory**: 32GB RAM (16GB per environment)
- **Storage**: 500GB SSD (250GB per environment)
- **Network**: 10Gbps bandwidth
- **OS**: Ubuntu 22.04 LTS

## 🗄️ Database Setup

### **MongoDB Cluster Setup**

#### **1. Install MongoDB**
```bash
# Import MongoDB public key
wget -qO - https://www.mongodb.org/static/pgp/server-7.0.asc | sudo apt-key add -

# Create list file
echo "deb [ arch=amd64,arm64 ] https://repo.mongodb.org/apt/ubuntu focal/mongodb-org/7.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-7.0.list

# Update package database
sudo apt-get update

# Install MongoDB
sudo apt-get install -y mongodb-org

# Start MongoDB
sudo systemctl start mongod
sudo systemctl enable mongod
```

#### **2. Configure MongoDB Replica Set**
```bash
# Edit MongoDB configuration
sudo nano /etc/mongod.conf

# Add replica set configuration
replication:
  replSetName: "imageviewer-rs"

# Restart MongoDB
sudo systemctl restart mongod

# Initialize replica set
mongosh --eval "rs.initiate()"
```

#### **3. Create Database and User**
```javascript
// Connect to MongoDB
mongosh

// Switch to admin database
use admin

// Create admin user
db.createUser({
  user: "admin",
  pwd: "secure_password",
  roles: ["userAdminAnyDatabase", "dbAdminAnyDatabase", "readWriteAnyDatabase"]
})

// Switch to imageviewer database
use imageviewer

// Create application user
db.createUser({
  user: "imageviewer",
  pwd: "app_password",
  roles: ["readWrite"]
})

// Create collections
db.createCollection("libraries")
db.createCollection("collections")
db.createCollection("mediaItems")
db.createCollection("users")
db.createCollection("userSettings")
db.createCollection("systemSettings")
db.createCollection("favoriteLists")
db.createCollection("backgroundJobs")
db.createCollection("auditLogs")
db.createCollection("errorLogs")
db.createCollection("backupHistory")
db.createCollection("performanceMetrics")
db.createCollection("userBehaviorEvents")
db.createCollection("userAnalytics")
db.createCollection("contentPopularity")
db.createCollection("searchAnalytics")
db.createCollection("userCollections")
db.createCollection("collectionRatings")
db.createCollection("userFollows")
db.createCollection("collectionComments")
db.createCollection("userMessages")
db.createCollection("conversations")
db.createCollection("torrents")
db.createCollection("downloadLinks")
db.createCollection("torrentStatistics")
db.createCollection("linkHealthChecker")
db.createCollection("downloadQualityOptions")
db.createCollection("distributionNodes")
db.createCollection("nodePerformanceMetrics")
db.createCollection("userRewards")
db.createCollection("rewardTransactions")
db.createCollection("rewardSettings")
db.createCollection("rewardAchievements")
db.createCollection("rewardBadges")
db.createCollection("premiumFeatures")
db.createCollection("userPremiumFeatures")
db.createCollection("storageLocations")
db.createCollection("fileStorageMapping")
db.createCollection("contentModeration")
db.createCollection("copyrightManagement")
db.createCollection("searchHistory")
db.createCollection("contentSimilarity")
db.createCollection("mediaProcessingJobs")
db.createCollection("customReports")
db.createCollection("userSecurity")
db.createCollection("notificationTemplates")
db.createCollection("notificationQueue")
db.createCollection("fileVersions")
db.createCollection("filePermissions")
db.createCollection("userGroups")
db.createCollection("userActivityLogs")
db.createCollection("systemHealth")
db.createCollection("systemMaintenance")

// Create indexes
db.libraries.createIndex({ "name": 1 })
db.libraries.createIndex({ "path": 1 })
db.libraries.createIndex({ "type": 1 })
db.libraries.createIndex({ "settings.enabled": 1 })
db.libraries.createIndex({ "statistics.lastScanDate": -1 })
db.libraries.createIndex({ "watchInfo.isWatching": 1 })
db.libraries.createIndex({ "searchIndex.tags": 1 })
db.libraries.createIndex({ "searchIndex.metadata": 1 })

db.collections.createIndex({ "libraryId": 1 })
db.collections.createIndex({ "name": 1 })
db.collections.createIndex({ "path": 1 })
db.collections.createIndex({ "type": 1 })
db.collections.createIndex({ "settings.enabled": 1 })
db.collections.createIndex({ "statistics.lastScanDate": -1 })
db.collections.createIndex({ "searchIndex.tags": 1 })
db.collections.createIndex({ "searchIndex.metadata": 1 })

db.mediaItems.createIndex({ "collectionId": 1 })
db.mediaItems.createIndex({ "name": 1 })
db.mediaItems.createIndex({ "path": 1 })
db.mediaItems.createIndex({ "type": 1 })
db.mediaItems.createIndex({ "settings.enabled": 1 })
db.mediaItems.createIndex({ "statistics.lastScanDate": -1 })
db.mediaItems.createIndex({ "searchIndex.tags": 1 })
db.mediaItems.createIndex({ "searchIndex.metadata": 1 })

// Create text indexes for search
db.libraries.createIndex({ "name": "text", "description": "text", "searchIndex.tags": "text" })
db.collections.createIndex({ "name": "text", "description": "text", "searchIndex.tags": "text" })
db.mediaItems.createIndex({ "name": "text", "description": "text", "searchIndex.tags": "text" })
```

## 🔄 Message Queue Setup

### **RabbitMQ Cluster Setup**

#### **1. Install RabbitMQ**
```bash
# Add RabbitMQ repository
curl -fsSL https://github.com/rabbitmq/signing-keys/releases/download/2.0/rabbitmq-release-signing-key.asc | sudo gpg --dearmor -o /usr/share/keyrings/rabbitmq-archive-keyring.gpg

# Add repository
echo "deb [signed-by=/usr/share/keyrings/rabbitmq-archive-keyring.gpg] https://dl.bintray.com/rabbitmq/debian $(lsb_release -sc) main" | sudo tee /etc/apt/sources.list.d/rabbitmq.list

# Update package database
sudo apt-get update

# Install RabbitMQ
sudo apt-get install -y rabbitmq-server

# Start RabbitMQ
sudo systemctl start rabbitmq-server
sudo systemctl enable rabbitmq-server
```

#### **2. Configure RabbitMQ**
```bash
# Enable management plugin
sudo rabbitmq-plugins enable rabbitmq_management

# Create admin user
sudo rabbitmqctl add_user admin secure_password
sudo rabbitmqctl set_user_tags admin administrator
sudo rabbitmqctl set_permissions -p / admin ".*" ".*" ".*"

# Create application user
sudo rabbitmqctl add_user imageviewer app_password
sudo rabbitmqctl set_user_tags imageviewer management
sudo rabbitmqctl set_permissions -p / imageviewer ".*" ".*" ".*"

# Create virtual hosts
sudo rabbitmqctl add_vhost imageviewer
sudo rabbitmqctl set_permissions -p imageviewer admin ".*" ".*" ".*"
sudo rabbitmqctl set_permissions -p imageviewer imageviewer ".*" ".*" ".*"
```

#### **3. Create Queues and Exchanges**
```bash
# Create exchanges
rabbitmqadmin -u admin -p secure_password -V imageviewer declare exchange name=collection.scan type=topic
rabbitmqadmin -u admin -p secure_password -V imageviewer declare exchange name=thumbnail.generation type=topic
rabbitmqadmin -u admin -p secure_password -V imageviewer declare exchange name=cache.generation type=topic
rabbitmqadmin -u admin -p secure_password -V imageviewer declare exchange name=collection.creation type=topic
rabbitmqadmin -u admin -p secure_password -V imageviewer declare exchange name=bulk.operation type=topic
rabbitmqadmin -u admin -p secure_password -V imageviewer declare exchange name=image.processing type=topic

# Create queues
rabbitmqadmin -u admin -p secure_password -V imageviewer declare queue name=collection.scan.queue durable=true
rabbitmqadmin -u admin -p secure_password -V imageviewer declare queue name=thumbnail.generation.queue durable=true
rabbitmqadmin -u admin -p secure_password -V imageviewer declare queue name=cache.generation.queue durable=true
rabbitmqadmin -u admin -p secure_password -V imageviewer declare queue name=collection.creation.queue durable=true
rabbitmqadmin -u admin -p secure_password -V imageviewer declare queue name=bulk.operation.queue durable=true
rabbitmqadmin -u admin -p secure_password -V imageviewer declare queue name=image.processing.queue durable=true

# Create dead letter queues
rabbitmqadmin -u admin -p secure_password -V imageviewer declare queue name=collection.scan.dlx durable=true
rabbitmqadmin -u admin -p secure_password -V imageviewer declare queue name=thumbnail.generation.dlx durable=true
rabbitmqadmin -u admin -p secure_password -V imageviewer declare queue name=cache.generation.dlx durable=true
rabbitmqadmin -u admin -p secure_password -V imageviewer declare queue name=collection.creation.dlx durable=true
rabbitmqadmin -u admin -p secure_password -V imageviewer declare queue name=bulk.operation.dlx durable=true
rabbitmqadmin -u admin -p secure_password -V imageviewer declare queue name=image.processing.dlx durable=true

# Bind queues to exchanges
rabbitmqadmin -u admin -p secure_password -V imageviewer declare binding source=collection.scan destination=collection.scan.queue routing_key=collection.scan
rabbitmqadmin -u admin -p secure_password -V imageviewer declare binding source=thumbnail.generation destination=thumbnail.generation.queue routing_key=thumbnail.generation
rabbitmqadmin -u admin -p secure_password -V imageviewer declare binding source=cache.generation destination=cache.generation.queue routing_key=cache.generation
rabbitmqadmin -u admin -p secure_password -V imageviewer declare binding source=collection.creation destination=collection.creation.queue routing_key=collection.creation
rabbitmqadmin -u admin -p secure_password -V imageviewer declare binding source=bulk.operation destination=bulk.operation.queue routing_key=bulk.operation
rabbitmqadmin -u admin -p secure_password -V imageviewer declare binding source=image.processing destination=image.processing.queue routing_key=image.processing
```

## 🗃️ Cache Setup

### **Redis Cluster Setup**

#### **1. Install Redis**
```bash
# Update package database
sudo apt-get update

# Install Redis
sudo apt-get install -y redis-server

# Start Redis
sudo systemctl start redis-server
sudo systemctl enable redis-server
```

#### **2. Configure Redis**
```bash
# Edit Redis configuration
sudo nano /etc/redis/redis.conf

# Configure Redis for production
bind 127.0.0.1
port 6379
timeout 300
tcp-keepalive 60
maxmemory 2gb
maxmemory-policy allkeys-lru
save 900 1
save 300 10
save 60 10000

# Restart Redis
sudo systemctl restart redis-server
```

#### **3. Configure Redis Authentication**
```bash
# Edit Redis configuration
sudo nano /etc/redis/redis.conf

# Add password
requirepass secure_redis_password

# Restart Redis
sudo systemctl restart redis-server

# Test connection
redis-cli -a secure_redis_password ping
```

## 📊 Monitoring Setup

### **Prometheus Setup**

#### **1. Install Prometheus**
```bash
# Create prometheus user
sudo useradd --no-create-home --shell /bin/false prometheus

# Create directories
sudo mkdir /etc/prometheus
sudo mkdir /var/lib/prometheus
sudo chown prometheus:prometheus /etc/prometheus
sudo chown prometheus:prometheus /var/lib/prometheus

# Download Prometheus
cd /tmp
wget https://github.com/prometheus/prometheus/releases/download/v2.45.0/prometheus-2.45.0.linux-amd64.tar.gz
tar xvf prometheus-2.45.0.linux-amd64.tar.gz
cd prometheus-2.45.0.linux-amd64

# Copy binaries
sudo cp prometheus /usr/local/bin/
sudo cp promtool /usr/local/bin/
sudo chown prometheus:prometheus /usr/local/bin/prometheus
sudo chown prometheus:prometheus /usr/local/bin/promtool

# Copy configuration
sudo cp -r consoles /etc/prometheus
sudo cp -r console_libraries /etc/prometheus
sudo chown -R prometheus:prometheus /etc/prometheus/consoles
sudo chown -R prometheus:prometheus /etc/prometheus/console_libraries
```

#### **2. Configure Prometheus**
```bash
# Create Prometheus configuration
sudo nano /etc/prometheus/prometheus.yml

# Add configuration
global:
  scrape_interval: 15s
  evaluation_interval: 15s

rule_files:
  - "alert_rules.yml"

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']
  
  - job_name: 'imageviewer-api'
    static_configs:
      - targets: ['localhost:7001']
    metrics_path: '/metrics'
    scrape_interval: 5s
  
  - job_name: 'imageviewer-worker'
    static_configs:
      - targets: ['localhost:7002']
    metrics_path: '/metrics'
    scrape_interval: 5s
  
  - job_name: 'mongodb'
    static_configs:
      - targets: ['localhost:9216']
  
  - job_name: 'redis'
    static_configs:
      - targets: ['localhost:9121']
  
  - job_name: 'rabbitmq'
    static_configs:
      - targets: ['localhost:9419']

# Create systemd service
sudo nano /etc/systemd/system/prometheus.service

# Add service configuration
[Unit]
Description=Prometheus
Wants=network-online.target
After=network-online.target

[Service]
User=prometheus
Group=prometheus
Type=simple
ExecStart=/usr/local/bin/prometheus \
    --config.file /etc/prometheus/prometheus.yml \
    --storage.tsdb.path /var/lib/prometheus/ \
    --web.console.templates=/etc/prometheus/consoles \
    --web.console.libraries=/etc/prometheus/console_libraries \
    --web.listen-address=0.0.0.0:9090 \
    --web.enable-lifecycle

[Install]
WantedBy=multi-user.target

# Start Prometheus
sudo systemctl daemon-reload
sudo systemctl start prometheus
sudo systemctl enable prometheus
```

### **Grafana Setup**

#### **1. Install Grafana**
```bash
# Add Grafana repository
wget -q -O - https://packages.grafana.com/gpg.key | sudo apt-key add -
echo "deb https://packages.grafana.com/oss/deb stable main" | sudo tee /etc/apt/sources.list.d/grafana.list

# Update package database
sudo apt-get update

# Install Grafana
sudo apt-get install -y grafana

# Start Grafana
sudo systemctl start grafana-server
sudo systemctl enable grafana-server
```

#### **2. Configure Grafana**
```bash
# Access Grafana
# URL: http://localhost:3000
# Default credentials: admin/admin

# Add Prometheus data source
# URL: http://localhost:9090

# Import dashboard
# Dashboard ID: 1860 (Node Exporter Full)
```

## 🔧 Application Configuration

### **Environment Variables**
```bash
# Create environment file
sudo nano /etc/imageviewer/environment

# Add configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80

# Database
ConnectionStrings__MongoDB=mongodb://imageviewer:app_password@localhost:27017/imageviewer
ConnectionStrings__Redis=localhost:6379,password=secure_redis_password

# Message Queue
RabbitMQ__HostName=localhost
RabbitMQ__Port=5672
RabbitMQ__UserName=imageviewer
RabbitMQ__Password=app_password
RabbitMQ__VirtualHost=imageviewer

# JWT
JWT__SecretKey=super-secret-key-for-jwt-tokens
JWT__Issuer=ImageViewer
JWT__Audience=ImageViewer
JWT__ExpirationMinutes=60

# File Storage
FileStorage__BasePath=/var/lib/imageviewer/files
FileStorage__ThumbnailPath=/var/lib/imageviewer/thumbnails
FileStorage__CachePath=/var/lib/imageviewer/cache

# Monitoring
Monitoring__Prometheus__Enabled=true
Monitoring__Prometheus__Port=9090
Monitoring__HealthChecks__Enabled=true
Monitoring__HealthChecks__Port=8080
```

### **Systemd Services**

#### **API Service**
```bash
# Create systemd service
sudo nano /etc/systemd/system/imageviewer-api.service

# Add service configuration
[Unit]
Description=ImageViewer API
After=network.target mongodb.service redis.service rabbitmq-server.service

[Service]
Type=simple
User=imageviewer
Group=imageviewer
WorkingDirectory=/opt/imageviewer/api
ExecStart=/usr/bin/dotnet ImageViewer.Api.dll
Restart=always
RestartSec=10
EnvironmentFile=/etc/imageviewer/environment

[Install]
WantedBy=multi-user.target

# Start service
sudo systemctl daemon-reload
sudo systemctl start imageviewer-api
sudo systemctl enable imageviewer-api
```

#### **Worker Service**
```bash
# Create systemd service
sudo nano /etc/systemd/system/imageviewer-worker.service

# Add service configuration
[Unit]
Description=ImageViewer Worker
After=network.target mongodb.service redis.service rabbitmq-server.service

[Service]
Type=simple
User=imageviewer
Group=imageviewer
WorkingDirectory=/opt/imageviewer/worker
ExecStart=/usr/bin/dotnet ImageViewer.Worker.dll
Restart=always
RestartSec=10
EnvironmentFile=/etc/imageviewer/environment

[Install]
WantedBy=multi-user.target

# Start service
sudo systemctl daemon-reload
sudo systemctl start imageviewer-worker
sudo systemctl enable imageviewer-worker
```

## 🔒 Security Configuration

### **Firewall Setup**
```bash
# Install UFW
sudo apt-get install -y ufw

# Configure firewall
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Allow SSH
sudo ufw allow ssh

# Allow HTTP/HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Allow application ports
sudo ufw allow 7001/tcp
sudo ufw allow 7002/tcp

# Allow monitoring ports
sudo ufw allow 9090/tcp
sudo ufw allow 3000/tcp

# Enable firewall
sudo ufw enable
```

### **SSL/TLS Configuration**
```bash
# Install Certbot
sudo apt-get install -y certbot

# Generate SSL certificate
sudo certbot certonly --standalone -d your-domain.com

# Configure Nginx with SSL
sudo nano /etc/nginx/sites-available/imageviewer

# Add SSL configuration
server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;

    ssl_certificate /etc/letsencrypt/live/your-domain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/your-domain.com/privkey.pem;

    location / {
        proxy_pass http://localhost:7001;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}

# Enable site
sudo ln -s /etc/nginx/sites-available/imageviewer /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

## 📈 Performance Optimization

### **System Optimization**
```bash
# Optimize kernel parameters
sudo nano /etc/sysctl.conf

# Add optimizations
net.core.somaxconn = 65535
net.core.netdev_max_backlog = 5000
net.ipv4.tcp_max_syn_backlog = 65535
net.ipv4.tcp_fin_timeout = 10
net.ipv4.tcp_tw_reuse = 1
net.ipv4.tcp_tw_recycle = 1
net.ipv4.tcp_keepalive_time = 1200
net.ipv4.tcp_keepalive_intvl = 15
net.ipv4.tcp_keepalive_probes = 5
vm.swappiness = 10
vm.dirty_ratio = 15
vm.dirty_background_ratio = 5

# Apply changes
sudo sysctl -p
```

### **MongoDB Optimization**
```bash
# Edit MongoDB configuration
sudo nano /etc/mongod.conf

# Add optimizations
storage:
  wiredTiger:
    engineConfig:
      cacheSizeGB: 4
      journalCompressor: snappy
      directoryForIndexes: true
    collectionConfig:
      blockCompressor: snappy
    indexConfig:
      prefixCompression: true

# Restart MongoDB
sudo systemctl restart mongod
```

### **Redis Optimization**
```bash
# Edit Redis configuration
sudo nano /etc/redis/redis.conf

# Add optimizations
tcp-keepalive 60
timeout 300
maxmemory 4gb
maxmemory-policy allkeys-lru
save 900 1
save 300 10
save 60 10000

# Restart Redis
sudo systemctl restart redis-server
```

## 🎯 Conclusion

Infrastructure setup guide này cung cấp comprehensive approach để setup ImageViewer Platform với:

1. **Database**: MongoDB cluster với replica set
2. **Message Queue**: RabbitMQ cluster với queues và exchanges
3. **Cache**: Redis cluster với authentication
4. **Monitoring**: Prometheus và Grafana setup
5. **Security**: Firewall, SSL/TLS, authentication
6. **Performance**: System và service optimizations

Setup này đảm bảo platform có infrastructure mạnh mẽ, secure, và có thể scale được.

---

**Created**: 2025-01-04
**Status**: Ready for Implementation
**Priority**: High
