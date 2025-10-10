# Settings Implementation Plan

## 📊 Current Settings Architecture

### ✅ Already Implemented

#### 1. **System Settings** (MongoDB - `system_settings` collection)
**Entity**: `SystemSetting` - Dynamic, database-driven configuration
**Storage**: MongoDB with full audit trail and version history
**Categories Currently in DB**:
- **Cache**: DefaultQuality (85), DefaultWidth (1920), DefaultHeight (1080), DefaultFormat (jpeg), PreserveOriginal, QualityPresets
- **Thumbnail**: DefaultSize (300), Quality (95), Format (jpeg)
- **BulkOperation**: DefaultQuality (85), DefaultFormat (jpeg), AutoScan, GenerateCache, GenerateThumbnails

**Service**: `ISystemSettingService` / `SystemSettingService`
**API**: ❌ **No controller yet - needs to be created**

#### 2. **User Settings** (MongoDB - `users` collection)
**Entity**: `User.Settings` (UserSettings ValueObject)
**Storage**: Embedded in User document
**Categories**:
- **Display**: DisplayMode, ItemsPerPage, Theme, Language, Timezone
- **Notifications**: Email, Push, SMS, InApp
- **Privacy**: ProfileVisibility, ActivityVisibility, DataSharing, Analytics
- **Performance**: ImageQuality, VideoQuality, CacheSize, AutoOptimize

**Service**: `IUserPreferencesService` / `UserPreferencesService`
**API**: ✅ `UserPreferencesController` (fully implemented)

#### 3. **Library Settings** (Per-library configuration)
**Entity**: `Library.Settings` (LibrarySettings ValueObject)
**API**: Via `LibrariesController`

#### 4. **Collection Settings** (Per-collection configuration)
**Entity**: `Collection.Settings` (CollectionSettings ValueObject)
**API**: Via `CollectionsController`

---

## 💡 Proposed Settings Screens

### Screen 1: 👤 User Settings Page
**Route**: `/settings/user` or `/settings` (default)
**Target Users**: All authenticated users
**Purpose**: Personal preferences and customization

#### Sections:

##### 🎨 **Display Preferences**
- View Mode: Grid / List / Card / Compact (dropdown)
- Items Per Page: 10 / 20 / 50 / 100 (select)
- Card Size: Mini / Tiny / Small / Medium / Large / XLarge (select)
- Compact Mode: Toggle
- Theme: Light / Dark / Auto (segmented control)
- Language: English / Chinese / Vietnamese (dropdown)
- Timezone: Auto-detect / Manual select (dropdown)
- Enable Animations: Toggle
- Enable Tooltips: Toggle

##### 🔔 **Notification Preferences**
- Email Notifications: Toggle
- Push Notifications: Toggle
- SMS Notifications: Toggle  
- In-App Notifications: Toggle
- Notification Types:
  - New Collection Added
  - Scan Completed
  - Thumbnail Generation Complete
  - Cache Generation Complete
  - System Alerts

##### 🔒 **Privacy Settings**
- Profile Visibility: Public / Friends / Private
- Show Online Status: Toggle
- Allow Direct Messages: Toggle
- Show Activity: Toggle
- Allow Search Indexing: Toggle
- Share Usage Data: Toggle
- Allow Analytics: Toggle
- Allow Cookies: Toggle

##### ⚡ **Performance Preferences**
- Image Quality Preference: Low / Medium / High / Original (select)
- Cache Size (MB): Slider (0-10000)
- Enable Lazy Loading: Toggle
- Enable Image Optimization: Toggle
- Max Concurrent Downloads: Slider (1-10)
- Enable Background Sync: Toggle
- Auto-save Interval (seconds): Input (10-300)
- Enable Compression: Toggle
- Enable Caching: Toggle

---

### Screen 2: 🏢 System Settings Page
**Route**: `/settings/system`
**Target Users**: Administrators only
**Purpose**: Global system configuration (stored in MongoDB)

#### Sections:

##### 🖼️ **Image Processing Settings**
Current in DB:
- Cache Default Quality: 85 (slider 0-100)
- Cache Default Width: 1920px (input)
- Cache Default Height: 1080px (input)
- Cache Default Format: JPEG / WebP / Original (select)
- Cache Preserve Original: Toggle
- Thumbnail Default Size: 300px (input)
- Thumbnail Quality: 95 (slider 0-100)
- Thumbnail Format: JPEG / WebP (select)

##### 📦 **Bulk Operation Settings**
Current in DB:
- Default Quality: 85 (slider)
- Default Format: JPEG / WebP (select)
- Auto Scan: Toggle
- Generate Cache: Toggle
- Generate Thumbnails: Toggle

##### 💾 **Cache Quality Presets**
Current in DB (JSON array):
- Perfect (100%) - JPEG
- High Quality (95%) - JPEG
- Optimized (85%) - JPEG (default)
- Medium (75%) - JPEG
- Low (60%) - JPEG
- WebP (85%) - WebP
- WebP High (95%) - WebP
- Original Quality - Keep original

**UI**: Editable table with columns: Name, Quality, Format, Description, Actions

##### 🔴 **Redis Cache Settings** (NEW - suggested)
- Max Memory: 48GB (display only from docker-compose)
- Default Expiration (minutes): 60 (input)
- Image Cache Expiration (minutes): 120 (input)
- Enable Compression: Toggle
- Sliding Expiration (minutes): 30 (input)
- Eviction Policy: LRU / LFU / FIFO (select)

##### 🐰 **RabbitMQ Settings** (NEW - suggested)
- Max Retry Count: 3 (input)
- Message Timeout (seconds): 1800 (input)
- Connection Timeout (seconds): 30 (input)
- Prefetch Count: 10 (input)
- Auto Acknowledge: Toggle

##### 🔐 **Security Settings** (NEW - suggested)
- Max Failed Login Attempts: 5 (input)
- Account Lockout Duration (minutes): 30 (input)
- Password Min Length: 8 (input)
- Require Strong Password: Toggle
- Session Timeout (hours): 24 (input)
- Enable Two-Factor: Toggle
- Backup Codes Count: 10 (input)

##### 📊 **Performance & Monitoring** (NEW - suggested)
- Enable Performance Monitoring: Toggle
- Performance Sample Rate: 1-100% (slider)
- Log Level: Debug / Info / Warning / Error (select)
- Log Retention Days: 30 (input)
- Enable Request Logging: Toggle
- Enable Slow Query Logging: Toggle
- Slow Query Threshold (ms): 1000 (input)

##### 🌐 **API Settings** (NEW - suggested)
- Default Page Size: 20 (input)
- Max Page Size: 100 (input)
- API Rate Limit (requests/minute): 100 (input)
- Enable CORS: Toggle
- Allowed Origins: Multi-line input
- Request Timeout (seconds): 30 (input)

##### 🎯 **Image Viewer Settings** (NEW - suggested)
- Default Slideshow Interval (seconds): 5 (slider 1-30)
- Enable Keyboard Shortcuts: Toggle
- Preload Adjacent Images: 1-10 (slider)
- Enable Fullscreen Mode: Toggle
- Enable Image Download: Toggle
- Show Image Metadata: Toggle
- Show EXIF Data: Toggle

---

## 🎨 UI Component Architecture

### Settings Layout Components:

```
<SettingsPage>
  <SettingsTabs>
    <Tab: User Settings />
    <Tab: System Settings (Admin) />
    <Tab: Library Settings />
    <Tab: Collection Settings />
  </SettingsTabs>
  
  <SettingsContent>
    <SettingsSection title="Display Preferences">
      <SettingItem label="Theme" component={Select} />
      <SettingItem label="View Mode" component={SegmentedControl} />
      <SettingItem label="Items Per Page" component={Select} />
    </SettingsSection>
    
    <SettingsSection title="Notifications">
      <SettingItem label="Email Notifications" component={Toggle} />
      ...
    </SettingsSection>
  </SettingsContent>
  
  <SettingsFooter>
    <Button variant="ghost">Reset to Defaults</Button>
    <Button variant="primary">Save Changes</Button>
  </SettingsFooter>
</SettingsPage>
```

### Reusable Components Needed:
1. **SettingsLayout.tsx** - Main container with tabs
2. **SettingsSection.tsx** - Collapsible section with title
3. **SettingItem.tsx** - Single setting row (label + control)
4. **Toggle.tsx** - Switch component
5. **Slider.tsx** - Range input
6. **Select.tsx** - Dropdown select
7. **SegmentedControl.tsx** - Button group selector
8. **MultiInput.tsx** - Tags/array input
9. **SaveIndicator.tsx** - Auto-save status

---

## 🚀 Implementation Steps

### Phase 1: Backend API (if needed)
1. ✅ System settings entity exists
2. ✅ System settings service exists
3. ❌ Create `SystemSettingsController` for CRUD operations
4. ❌ Add new system settings categories to seed data

### Phase 2: Frontend Components
1. Create reusable form components (Toggle, Slider, Select, etc.)
2. Create SettingsLayout with tab navigation
3. Create SettingsSection for collapsible groups
4. Create SettingItem for consistent layout

### Phase 3: User Settings Screen
1. Implement UserSettings tab
2. Connect to `/api/v1/userpreferences` API
3. Add form validation
4. Implement auto-save with debounce

### Phase 4: System Settings Screen
1. Implement SystemSettings tab (admin only)
2. Create SystemSettings API controller
3. Connect to system settings endpoints
4. Add permission checks

### Phase 5: Testing & Polish
1. Test all settings save/load
2. Test validation rules
3. Test permission controls
4. Add loading states and error handling

---

## 📝 Recommended Additions to System Settings

### Category: **ImageProcessing**
- `SmartQuality.Enabled` (boolean) - Enable smart quality adjustment
- `SmartQuality.MinQuality` (integer) - Minimum quality threshold (60)
- `SmartQuality.MaxQuality` (integer) - Maximum quality threshold (100)
- `SmartQuality.PreserveLowQuality` (boolean) - Don't enhance low quality images
- `Resize.MaxDimension` (integer) - Max dimension before resize (4096)
- `Resize.PreserveAspectRatio` (boolean) - Always preserve aspect ratio

### Category: **Storage**
- `CacheFolders.MaxTotal` (integer) - Max total cache folders (10)
- `CacheFolders.DefaultPriority` (integer) - Default folder priority (5)
- `CacheFolders.AutoBalance` (boolean) - Auto-balance across folders
- `Cleanup.AutoCleanup` (boolean) - Enable automatic cleanup
- `Cleanup.IntervalHours` (integer) - Cleanup interval (24)
- `Cleanup.RetentionDays` (integer) - Cache retention days (30)

### Category: **Worker**
- `Worker.MaxConcurrentJobs` (integer) - Max concurrent background jobs (5)
- `Worker.JobTimeout` (integer) - Job timeout in minutes (60)
- `Worker.RetryDelay` (integer) - Retry delay in seconds (30)
- `Worker.EnableJobMonitoring` (boolean) - Enable job monitoring service
- `Worker.MonitoringInterval` (integer) - Monitoring check interval in seconds (5)

### Category: **UI**
- `UI.DefaultViewMode` (string) - Default view mode (grid/list/detail)
- `UI.DefaultCardSize` (string) - Default card size (medium)
- `UI.EnableCompactMode` (boolean) - Enable compact mode by default
- `UI.DefaultPageSize` (integer) - Default pagination size (20)
- `UI.ShowThumbnails` (boolean) - Show thumbnails in lists
- `UI.ShowMetadata` (boolean) - Show metadata by default

---

## 🎯 Priority Recommendations

### Must Have (Phase 1):
1. ✅ User Display Settings (already implemented)
2. ❌ System Image Processing Settings UI
3. ❌ System Cache Quality Presets UI

### Should Have (Phase 2):
4. ❌ User Notification Settings UI
5. ❌ User Privacy Settings UI
6. ❌ System Redis Cache Settings UI
7. ❌ System Worker Settings UI

### Nice to Have (Phase 3):
8. ❌ Library Settings UI (with library selector)
9. ❌ Collection Settings UI (with collection selector)
10. ❌ System API/Security Settings UI

---

## 💬 Questions for User

1. **Which settings screen should we implement first?**
   - A) User Settings (personal preferences)
   - B) System Settings (admin configuration)
   - C) Both simultaneously

2. **What specific system settings are most important for you?**
   - Cache quality presets?
   - Redis configuration?
   - Worker job settings?
   - Image processing defaults?

3. **Should we create the SystemSettings API controller first, or start with the UI?**

4. **Do you want authentication/authorization for settings pages?**
   - Public access for user settings?
   - Admin-only for system settings?

