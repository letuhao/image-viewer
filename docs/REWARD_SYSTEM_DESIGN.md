# Reward System Design - Image Viewer Platform

## 📋 Tổng Quan

Hệ thống điểm thưởng (Reward System) là một tính năng quan trọng để khuyến khích người dùng đóng góp vào platform và tạo ra một ecosystem bền vững. Hệ thống này bao gồm việc kiếm điểm, tiêu thụ điểm, và quản lý các tính năng premium.

## 🎯 Mục Tiêu

### 1. **Khuyến Khích Đóng Góp**
- Khuyến khích người dùng upload collections chất lượng cao
- Thúc đẩy việc seeding torrents và vận hành distribution nodes
- Tạo động lực cho việc tạo tags và nội dung hữu ích
- Khuyến khích tương tác xã hội tích cực

### 2. **Tạo Giá Trị**
- Cung cấp quyền truy cập vào các tính năng premium
- Cho phép download với chất lượng cao và tốc độ nhanh
- Tạo ra một hệ thống kinh tế nội bộ
- Xây dựng cộng đồng gắn kết

### 3. **Quản Lý Tài Nguyên**
- Phân phối tài nguyên một cách công bằng
- Kiểm soát việc sử dụng bandwidth và storage
- Tối ưu hóa hiệu suất hệ thống
- Ngăn chặn lạm dụng

## 🏗️ Kiến Trúc Hệ Thống

### Database Collections (7 Collections)

#### **1. User Rewards Collection**
- **Mục đích**: Lưu trữ thông tin điểm thưởng của từng người dùng
- **Dữ liệu chính**: Current points, total earned, level, badges, achievements
- **Thống kê**: Upload, seeding, node operation, tag creation, torrent creation, social

#### **2. Reward Transactions Collection**
- **Mục đích**: Theo dõi tất cả giao dịch điểm thưởng
- **Dữ liệu chính**: Transaction type, points, category, action, metadata
- **Trạng thái**: Pending, completed, cancelled, refunded

#### **3. Reward Settings Collection**
- **Mục đích**: Cấu hình hệ thống điểm thưởng
- **Dữ liệu chính**: Earning/spending settings, multipliers, limits, requirements
- **Quản lý**: Active/inactive, effective dates, created by

#### **4. Reward Achievements Collection**
- **Mục đích**: Định nghĩa các thành tựu có thể đạt được
- **Dữ liệu chính**: Requirements, rewards, rarity, category
- **Tính năng**: Hidden achievements, milestone tracking

#### **5. Reward Badges Collection**
- **Mục đích**: Hệ thống huy hiệu và danh hiệu
- **Dữ liệu chính**: Requirements, benefits, rarity, display priority
- **Tính năng**: Limited time badges, seasonal badges

#### **6. Premium Features Collection**
- **Mục đích**: Định nghĩa các tính năng premium có thể mua bằng điểm
- **Dữ liệu chính**: Pricing, features, requirements, type
- **Phân loại**: Download, upload, social, analytics, customization

#### **7. User Premium Features Collection**
- **Mục đích**: Theo dõi các tính năng premium mà người dùng đã mua
- **Dữ liệu chính**: Purchase info, usage statistics, settings
- **Trạng thái**: Active, expired, cancelled, pending

## 💰 Hệ Thống Kiếm Điểm

### 1. **Upload Collections**
```javascript
// Base points calculation
basePoints = 10 + (fileCount * 2) + (totalSize / 1024 / 1024 / 10) // 1 point per 10MB

// Quality multipliers
qualityMultiplier = {
  high: 1.5,    // Rating >= 4.0
  medium: 1.0,  // Rating >= 3.0
  low: 0.5      // Rating < 3.0
}

// Popularity multipliers
popularityMultiplier = {
  viral: 2.0,   // > 1000 views in 24h
  popular: 1.5, // > 100 views in 24h
  normal: 1.0   // < 100 views in 24h
}

// User level multipliers
userMultiplier = {
  newUser: 1.5,  // First 30 days
  regular: 1.0,  // Regular users
  premium: 1.2   // Premium users
}

// Final calculation
finalPoints = basePoints * qualityMultiplier * popularityMultiplier * userMultiplier
```

### 2. **Seeding Torrents**
```javascript
// Base points per hour
basePointsPerHour = 5

// Ratio multipliers
ratioMultiplier = {
  excellent: 2.0, // Ratio >= 2.0
  good: 1.5,      // Ratio >= 1.5
  fair: 1.0,      // Ratio >= 1.0
  poor: 0.5       // Ratio < 1.0
}

// Size multipliers
sizeMultiplier = {
  large: 1.5,   // > 1GB
  medium: 1.0,  // 100MB - 1GB
  small: 0.5    // < 100MB
}

// Final calculation
finalPoints = basePointsPerHour * hours * ratioMultiplier * sizeMultiplier
```

### 3. **Operating Distribution Nodes**
```javascript
// Base points per hour
basePointsPerHour = 10

// Quality multipliers
qualityMultiplier = {
  excellent: 2.0, // Quality score >= 90
  good: 1.5,      // Quality score >= 80
  fair: 1.0,      // Quality score >= 70
  poor: 0.5       // Quality score < 70
}

// Uptime multipliers
uptimeMultiplier = {
  excellent: 1.5, // Uptime >= 99%
  good: 1.2,      // Uptime >= 95%
  fair: 1.0,      // Uptime >= 90%
  poor: 0.5       // Uptime < 90%
}

// Final calculation
finalPoints = basePointsPerHour * hours * qualityMultiplier * uptimeMultiplier
```

### 4. **Creating Tags**
```javascript
// Base points per tag
basePoints = 5

// Usage multipliers
usageMultiplier = {
  popular: 2.0,  // Used by > 100 users
  common: 1.5,   // Used by > 10 users
  normal: 1.0    // Used by < 10 users
}

// Quality multipliers
qualityMultiplier = {
  high: 1.5,     // Descriptive, relevant
  medium: 1.0,   // Standard tags
  low: 0.5       // Generic, spam
}

// Final calculation
finalPoints = basePoints * usageMultiplier * qualityMultiplier
```

### 5. **Creating Torrents**
```javascript
// Base points per torrent
basePoints = 20

// Size multipliers
sizeMultiplier = {
  large: 2.0,   // > 5GB
  medium: 1.5,  // 1GB - 5GB
  small: 1.0    // < 1GB
}

// Quality multipliers
qualityMultiplier = {
  high: 1.5,    // High quality content
  medium: 1.0,  // Standard quality
  low: 0.5      // Low quality
}

// Final calculation
finalPoints = basePoints * sizeMultiplier * qualityMultiplier
```

### 6. **Social Interactions**
```javascript
// Comment points
commentPoints = {
  helpful: 2,    // Marked as helpful
  normal: 1,     // Regular comment
  spam: -5       // Marked as spam
}

// Rating points
ratingPoints = {
  detailed: 3,   // Rating with review
  simple: 1,     // Rating only
  spam: -5       // Spam rating
}

// Share points
sharePoints = {
  collection: 2, // Share collection
  comment: 1     // Share comment
}
```

## 🛒 Hệ Thống Tiêu Thụ Điểm

### 1. **Premium Downloads**
```javascript
// Base cost calculation
baseCost = 10 + (fileSize / 1024 / 1024 / 100) // 1 point per 100MB

// Quality multipliers
qualityMultiplier = {
  original: 1.0,  // Original quality
  high: 0.8,      // High quality
  medium: 0.6,    // Medium quality
  low: 0.4        // Low quality
}

// Speed multipliers
speedMultiplier = {
  fast: 1.5,      // Fast download
  normal: 1.0,    // Normal speed
  slow: 0.5       // Slow speed
}

// Priority multipliers
priorityMultiplier = {
  high: 2.0,      // High priority
  normal: 1.0,    // Normal priority
  low: 0.5        // Low priority
}

// Final cost
finalCost = baseCost * qualityMultiplier * speedMultiplier * priorityMultiplier
```

### 2. **Premium Features**
```javascript
// Feature pricing examples
premiumFeatures = {
  fastDownload: {
    cost: 50,
    duration: 24, // hours
    type: "duration"
  },
  highQualityDownload: {
    cost: 100,
    duration: 24, // hours
    type: "duration"
  },
  priorityProcessing: {
    cost: 200,
    duration: 7, // days
    type: "duration"
  },
  customWatermark: {
    cost: 500,
    maxUses: 10,
    type: "one_time"
  },
  advancedAnalytics: {
    cost: 1000,
    duration: 30, // days
    type: "duration"
  }
}
```

### 3. **Social Features**
```javascript
// Social feature pricing
socialFeatures = {
  customProfile: {
    cost: 300,
    duration: 30, // days
    type: "duration"
  },
  prioritySupport: {
    cost: 500,
    duration: 30, // days
    type: "duration"
  },
  exclusiveContent: {
    cost: 1000,
    duration: 30, // days
    type: "duration"
  }
}
```

## 🏆 Hệ Thống Level & Achievements

### 1. **User Levels**
```javascript
levels = {
  bronze: {
    minPoints: 0,
    maxPoints: 999,
    benefits: ["Basic features", "Standard support"],
    multiplier: 1.0
  },
  silver: {
    minPoints: 1000,
    maxPoints: 4999,
    benefits: ["Priority support", "Advanced analytics"],
    multiplier: 1.1
  },
  gold: {
    minPoints: 5000,
    maxPoints: 19999,
    benefits: ["Premium features", "Custom profile"],
    multiplier: 1.2
  },
  platinum: {
    minPoints: 20000,
    maxPoints: 99999,
    benefits: ["VIP support", "Exclusive content"],
    multiplier: 1.3
  },
  diamond: {
    minPoints: 100000,
    maxPoints: Infinity,
    benefits: ["All features", "Personal manager"],
    multiplier: 1.5
  }
}
```

### 2. **Achievements**
```javascript
achievements = {
  // Upload achievements
  firstUpload: {
    name: "First Steps",
    description: "Upload your first collection",
    points: 100,
    rarity: "common"
  },
  uploadMaster: {
    name: "Upload Master",
    description: "Upload 100 collections",
    points: 1000,
    rarity: "rare"
  },
  
  // Seeding achievements
  seeder: {
    name: "Seeder",
    description: "Seed 1TB of data",
    points: 500,
    rarity: "uncommon"
  },
  superSeeder: {
    name: "Super Seeder",
    description: "Seed 10TB of data",
    points: 2000,
    rarity: "epic"
  },
  
  // Node achievements
  nodeOperator: {
    name: "Node Operator",
    description: "Operate a node for 1000 hours",
    points: 1000,
    rarity: "rare"
  },
  superNode: {
    name: "Super Node",
    description: "Operate a node with 99% uptime for 30 days",
    points: 3000,
    rarity: "legendary"
  },
  
  // Social achievements
  helpfulUser: {
    name: "Helpful User",
    description: "Get 100 helpful votes on comments",
    points: 500,
    rarity: "uncommon"
  },
  communityLeader: {
    name: "Community Leader",
    description: "Get 1000 followers",
    points: 2000,
    rarity: "epic"
  }
}
```

### 3. **Badges**
```javascript
badges = {
  // Upload badges
  uploader: {
    name: "Uploader",
    description: "Upload 10 collections",
    icon: "upload-icon",
    rarity: "common"
  },
  qualityUploader: {
    name: "Quality Uploader",
    description: "Upload 10 collections with 4+ rating",
    icon: "quality-icon",
    rarity: "uncommon"
  },
  
  // Seeding badges
  seeder: {
    name: "Seeder",
    description: "Maintain 2.0+ ratio for 30 days",
    icon: "seeder-icon",
    rarity: "uncommon"
  },
  superSeeder: {
    name: "Super Seeder",
    description: "Maintain 5.0+ ratio for 90 days",
    icon: "super-seeder-icon",
    rarity: "rare"
  },
  
  // Node badges
  nodeOperator: {
    name: "Node Operator",
    description: "Operate a node for 30 days",
    icon: "node-icon",
    rarity: "uncommon"
  },
  superNode: {
    name: "Super Node",
    description: "Operate a node with 99% uptime",
    icon: "super-node-icon",
    rarity: "rare"
  }
}
```

## ⚙️ Cấu Hình Hệ Thống

### 1. **Reward Settings**
```javascript
rewardSettings = {
  // Earning limits
  limits: {
    daily: 1000,      // Max points per day
    weekly: 5000,     // Max points per week
    monthly: 20000,   // Max points per month
    perAction: 500    // Max points per single action
  },
  
  // Quality requirements
  requirements: {
    minQuality: 3.0,  // Minimum quality score
    minSize: 1024,    // Minimum file size (1KB)
    minDuration: 1,   // Minimum duration for seeding (hours)
    minUptime: 90     // Minimum uptime for nodes (%)
  },
  
  // Bonus settings
  bonuses: {
    firstUpload: 100,     // Bonus for first collection
    milestoneUploads: [   // Milestone bonuses
      { count: 10, bonus: 200 },
      { count: 50, bonus: 500 },
      { count: 100, bonus: 1000 }
    ],
    consecutiveDays: [    // Streak bonuses
      { days: 7, bonus: 100 },
      { days: 30, bonus: 500 },
      { days: 90, bonus: 1500 }
    ],
    referral: 500,        // Referral bonus
    anniversary: 1000     // Account anniversary bonus
  },
  
  // Penalty settings
  penalties: {
    lowQuality: -50,      // Low quality content
    spam: -100,           // Spam content
    abuse: -200,          // Abuse behavior
    inactive: -10         // Inactive account (per day)
  }
}
```

### 2. **Premium Features Settings**
```javascript
premiumFeatures = {
  // Download features
  download: {
    fastDownload: {
      cost: 50,
      duration: 24,
      features: {
        speed: "fast",
        priority: "high",
        bandwidth: 100000000 // 100MB/s
      }
    },
    highQualityDownload: {
      cost: 100,
      duration: 24,
      features: {
        quality: "original",
        speed: "normal",
        priority: "normal"
      }
    }
  },
  
  // Upload features
  upload: {
    priorityProcessing: {
      cost: 200,
      duration: 7,
      features: {
        priority: "high",
        maxSize: 10737418240, // 10GB
        maxFiles: 1000
      }
    },
    customWatermark: {
      cost: 500,
      maxUses: 10,
      features: {
        watermark: true,
        customText: true,
        position: "customizable"
      }
    }
  },
  
  // Social features
  social: {
    customProfile: {
      cost: 300,
      duration: 30,
      features: {
        customLayout: true,
        advancedStats: true,
        customTheme: true
      }
    },
    prioritySupport: {
      cost: 500,
      duration: 30,
      features: {
        priority: "high",
        responseTime: 2, // hours
        dedicatedSupport: true
      }
    }
  }
}
```

## 📊 Analytics & Monitoring

### 1. **Key Metrics**
```javascript
metrics = {
  // User engagement
  dailyActiveUsers: "Number of users earning points daily",
  averagePointsPerUser: "Average points earned per user",
  pointsDistribution: "Distribution of points across users",
  
  // System health
  pointsInCirculation: "Total points in circulation",
  pointsEarnedPerDay: "Total points earned per day",
  pointsSpentPerDay: "Total points spent per day",
  
  // Feature usage
  premiumFeatureUsage: "Usage of premium features",
  achievementCompletion: "Achievement completion rates",
  levelDistribution: "Distribution of user levels",
  
  // Economic health
  pointsVelocity: "Rate of points circulation",
  inflationRate: "Rate of points inflation",
  deflationRate: "Rate of points deflation"
}
```

### 2. **Monitoring Queries**
```javascript
// Daily points flow
db.rewardTransactions.aggregate([
  {
    $match: {
      status: "completed",
      createdAt: { $gte: new Date(Date.now() - 24 * 60 * 60 * 1000) }
    }
  },
  {
    $group: {
      _id: null,
      totalEarned: { $sum: { $cond: [{ $gt: ["$points", 0] }, "$points", 0] } },
      totalSpent: { $sum: { $cond: [{ $lt: ["$points", 0] }, { $abs: "$points" }, 0] } },
      transactionCount: { $sum: 1 }
    }
  }
]);

// Top earners by category
db.rewardTransactions.aggregate([
  {
    $match: {
      transactionType: "earn",
      status: "completed",
      createdAt: { $gte: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000) }
    }
  },
  {
    $group: {
      _id: { userId: "$userId", category: "$category" },
      totalPoints: { $sum: "$points" },
      transactionCount: { $sum: 1 }
    }
  },
  {
    $sort: { totalPoints: -1 }
  },
  {
    $limit: 50
  }
]);

// Achievement completion rates
db.rewardAchievements.aggregate([
  {
    $lookup: {
      from: "userRewards",
      localField: "achievementId",
      foreignField: "achievements.achievementId",
      as: "completedBy"
    }
  },
  {
    $addFields: {
      completionCount: { $size: "$completedBy" }
    }
  },
  {
    $sort: { completionCount: -1 }
  }
]);
```

## 🚀 Implementation Roadmap

### Phase 1: Core Reward System (4-6 weeks)
- [ ] User rewards collection và basic CRUD
- [ ] Reward transactions tracking
- [ ] Basic earning mechanisms (upload, seeding)
- [ ] Simple spending system (premium downloads)

### Phase 2: Advanced Features (3-4 weeks)
- [ ] Achievement system
- [ ] Badge system
- [ ] Level system
- [ ] Advanced multipliers

### Phase 3: Premium Features (4-5 weeks)
- [ ] Premium features catalog
- [ ] Purchase system
- [ ] Usage tracking
- [ ] Feature management

### Phase 4: Analytics & Optimization (2-3 weeks)
- [ ] Analytics dashboard
- [ ] Performance monitoring
- [ ] A/B testing framework
- [ ] Economic balancing

### Phase 5: Advanced Features (3-4 weeks)
- [ ] Referral system
- [ ] Seasonal events
- [ ] Limited time offers
- [ ] Community challenges

## 🎯 Success Metrics

### User Engagement
- **Daily Active Users**: Target 80% of users earning points daily
- **Points Earned**: Target 10,000+ points earned per day
- **Achievement Completion**: Target 60% completion rate
- **Level Progression**: Target 40% of users reaching Silver level

### Economic Health
- **Points Circulation**: Target 1M+ points in circulation
- **Premium Usage**: Target 20% of users using premium features
- **Feature Adoption**: Target 50% adoption rate for new features
- **User Retention**: Target 85% monthly retention

### System Performance
- **Transaction Processing**: Target <100ms average
- **Analytics Generation**: Target <5s for complex queries
- **Feature Availability**: Target 99.9% uptime
- **Data Consistency**: Target 100% transaction consistency

## 🔧 Technical Requirements

### Infrastructure
- **MongoDB Cluster**: Sharded cluster for scalability
- **Redis Cache**: Caching for frequent queries
- **Message Queue**: RabbitMQ for async processing
- **Monitoring**: Comprehensive monitoring và alerting

### Security
- **Transaction Security**: Atomic transactions
- **Fraud Prevention**: Anti-gaming mechanisms
- **Data Integrity**: Consistent data across collections
- **Audit Trail**: Complete transaction history

### Performance
- **Database Optimization**: Proper indexing strategy
- **Caching Strategy**: Multi-level caching
- **Async Processing**: Background job processing
- **Load Balancing**: Smart load balancing

## 🎉 Conclusion

Hệ thống điểm thưởng sẽ tạo ra một ecosystem bền vững và gắn kết, khuyến khích người dùng đóng góp chất lượng cao và tạo ra giá trị cho cộng đồng. Với thiết kế linh hoạt và có thể mở rộng, hệ thống này sẽ phát triển cùng với platform và đáp ứng nhu cầu của người dùng trong tương lai.

### Key Benefits:
1. **User Engagement**: Tăng cường sự tham gia của người dùng
2. **Quality Content**: Khuyến khích nội dung chất lượng cao
3. **Community Building**: Xây dựng cộng đồng gắn kết
4. **Economic Sustainability**: Tạo ra hệ thống kinh tế bền vững
5. **Feature Adoption**: Thúc đẩy việc sử dụng các tính năng mới
6. **User Retention**: Tăng cường khả năng giữ chân người dùng
