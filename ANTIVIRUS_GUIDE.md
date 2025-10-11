# 🛡️ Antivirus False Positive Guide

## ❓ Why Is My Antivirus Blocking the Scripts?

The PowerShell scripts use **legitimate development tools** that happen to match patterns used by malware. This causes **false positives**.

---

## 🔍 What Triggers Antivirus Detection

### **1. Process Termination** 🚨
```powershell
Stop-Process -Id $proc.Id -Force
```
**Why flagged**: Malware often kills security processes  
**Our use**: Stopping old dev server instances before restart  
**Legitimate**: Yes, common in dev scripts

### **2. Hidden Windows** 🚨
```powershell
Start-Process -WindowStyle Hidden
```
**Why flagged**: Malware runs hidden to avoid detection  
**Our use**: Running background services without console clutter  
**Legitimate**: Yes, standard for daemon processes

### **3. Process Inspection** 🚨
```powershell
Get-CimInstance Win32_Process -Filter "ProcessId = ..."
```
**Why flagged**: Spyware reads process information  
**Our use**: Identifying our own Node.js processes  
**Legitimate**: Yes, process management

### **4. Mass Process Enumeration** 🚨
```powershell
Get-Process | Where-Object {$_.ProcessName -like "*ImageViewer*"}
```
**Why flagged**: Malware scans for antivirus/security processes  
**Our use**: Finding our own services to stop them  
**Legitimate**: Yes, administrative task

---

## ✅ Solutions (Pick One)

### **Solution 1: Add Antivirus Exception** ⭐ **RECOMMENDED**

**Windows Defender**:
1. Open Windows Security
2. Go to "Virus & threat protection"
3. Click "Manage settings"
4. Scroll to "Exclusions"
5. Click "Add or remove exclusions"
6. Add folder: `D:\Works\source\image-viewer`

**Other Antivirus** (Norton, McAfee, Kaspersky, etc.):
1. Open antivirus settings
2. Find "Exceptions" or "Exclusions"
3. Add: `D:\Works\source\image-viewer\*.ps1`
4. Or add entire folder: `D:\Works\source\image-viewer`

**Why this is safe**:
- ✅ You control this folder (your development code)
- ✅ Scripts are transparent (you can read the source)
- ✅ Only affects this project
- ✅ Can be removed anytime

---

### **Solution 2: Use Safe Mode Script** ⭐ **ANTIVIRUS-FRIENDLY**

**Use the alternative script**:
```powershell
.\start-all-services-safe.ps1
```

**Differences**:
- ✅ Shows visible console windows (not hidden)
- ✅ No process termination
- ✅ Minimal process inspection
- ✅ Less likely to trigger antivirus

**Trade-off**:
- ⚠️ You'll see 4 console windows on your desktop
- ⚠️ Must manually close windows to stop services
- ⚠️ Can't run truly in background

**Good for**: Strict corporate environments, paranoid antivirus

---

### **Solution 3: Change Execution Policy** ⚙️ **TECHNICAL**

**Check current policy**:
```powershell
Get-ExecutionPolicy
```

**If `Restricted`, change to `RemoteSigned`**:
```powershell
# Run PowerShell as Administrator
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Why this helps**:
- Scripts you write locally are trusted
- Only blocks downloaded scripts (safer)

---

### **Solution 4: Sign the Script** 🔐 **ADVANCED**

If your organization requires signed scripts:

```powershell
# 1. Create self-signed certificate (run as Admin)
$cert = New-SelfSignedCertificate -Subject "CN=ImageViewer Dev" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -Type CodeSigningCert

# 2. Sign the script
Set-AuthenticodeSignature -FilePath .\start-all-services.ps1 -Certificate $cert

# 3. Trust the certificate
Export-Certificate -Cert $cert -FilePath ImageViewerDev.cer
Import-Certificate -FilePath ImageViewerDev.cer -CertStoreLocation Cert:\CurrentUser\Root
```

---

### **Solution 5: Run Services Manually** 🔧 **FALLBACK**

If scripts are completely blocked, run services in separate terminals:

**Terminal 1 - API**:
```powershell
cd src\ImageViewer.Api
dotnet run
```

**Terminal 2 - Worker**:
```powershell
cd src\ImageViewer.Worker
dotnet run
```

**Terminal 3 - Scheduler**:
```powershell
cd src\ImageViewer.Scheduler
dotnet run
```

**Terminal 4 - Frontend**:
```powershell
cd client
npm run dev
```

---

## 🔒 Security Verification

### **How to Verify Scripts Are Safe**

1. **Read the source code** - All scripts are plain text
2. **Check what they do**:
   - Start .NET applications (`dotnet run`)
   - Start Node.js dev server (`npm run dev`)
   - Check service status
   - View log files
   - Nothing suspicious!

3. **Scan with antivirus** (if you trust it):
   ```powershell
   # Windows Defender scan
   Start-MpScan -ScanPath "D:\Works\source\image-viewer" -ScanType QuickScan
   ```

4. **Upload to VirusTotal** (optional):
   - Go to https://www.virustotal.com
   - Upload `start-all-services.ps1`
   - See analysis from 70+ antivirus engines

---

## 📋 Recommended Approach

### **For Most Users**: Solution 1 (Add Exception)
```
1. Add folder exception in Windows Defender
2. Use .\start-all-services.ps1 normally
3. Enjoy background execution
```

### **For Corporate/Managed PCs**: Solution 2 (Safe Mode)
```
1. Use .\start-all-services-safe.ps1
2. Accept visible console windows
3. Works without admin rights
```

### **For Maximum Control**: Solution 5 (Manual)
```
1. Open 4 separate terminals
2. Run each service manually
3. Full visibility of all output
```

---

## ⚠️ Common Antivirus Products & Behavior

| Antivirus | Typical Behavior | Recommendation |
|-----------|------------------|----------------|
| **Windows Defender** | May flag, easy to whitelist | Add folder exception |
| **Norton** | Often blocks hidden processes | Use -Visible or safe mode |
| **McAfee** | Flags process management | Add script exception |
| **Kaspersky** | Strict, may block signing | Use safe mode or manual |
| **Avast/AVG** | Moderate, usually allows | Add folder exception |
| **Bitdefender** | Strict on hidden processes | Use safe mode |

---

## 🧪 Test If Scripts Are Blocked

```powershell
# Test 1: Can you run PowerShell?
Get-ExecutionPolicy
# Should be: RemoteSigned or Unrestricted

# Test 2: Can you start a simple process?
Start-Process notepad
# Should open Notepad

# Test 3: Can you run the script?
.\start-all-services-safe.ps1 -SkipBuild
# Should start services

# If Test 3 fails, check antivirus quarantine logs
```

---

## 📞 Corporate IT Guidelines

If you're on a corporate/managed PC and scripts are blocked:

### **Option A: Request Exception**

Email to IT department:
```
Subject: Whitelist Request for Development Scripts

Hi IT Team,

I'm developing the ImageViewer application and need to run local development servers.
The PowerShell scripts in D:\Works\source\image-viewer\ are being flagged by antivirus.

These scripts:
- Start .NET and Node.js development servers
- Manage local processes (no network activity)
- Are part of our internal development workflow

Could you please whitelist:
- Folder: D:\Works\source\image-viewer\
- Or files: start-all-services.ps1, stop-all-services.ps1

Source code is available for review at: [link to your repo]

Thank you!
```

### **Option B: Use Docker Instead**

If scripts are blocked but Docker is allowed:
```powershell
docker-compose up -d
```

Docker is often whitelisted by corporate IT.

### **Option C: Manual Startup**

Use Solution 5 (4 separate terminals) - No scripts needed.

---

## 🎓 Understanding the False Positive

### **Why Antivirus Gets It Wrong**

Antivirus uses **heuristic detection** - pattern matching:

| Pattern | Malware Use | Our Use | False Positive? |
|---------|-------------|---------|-----------------|
| Kill processes | Kill antivirus | Restart dev servers | ✅ Yes |
| Hidden windows | Hide from user | Background services | ✅ Yes |
| Process scanning | Find targets | Find our processes | ✅ Yes |
| Start executables | Install malware | Run .NET/Node | ❌ Less likely |
| Network connections | C&C communication | RabbitMQ/MongoDB | ❌ Not flagged |

**The combination** of these patterns triggers detection, even though each is legitimate.

---

## 🔧 Debug Antivirus Blocks

### **Windows Defender Logs**

1. Open Windows Security
2. Go to "Virus & threat protection"
3. Click "Protection history"
4. Look for recent blocks
5. See what was flagged

### **View Specific Block**:
```powershell
Get-MpThreatDetection | Select-Object -First 5 | Format-List
```

### **Check if PowerShell is blocked**:
```powershell
Get-MpPreference | Select-Object -ExpandProperty ExclusionPath
```

### **Temporarily disable (for testing)**:
```powershell
# Run as Administrator
Set-MpPreference -DisableRealtimeMonitoring $true

# Test your script
.\start-all-services.ps1

# Re-enable immediately
Set-MpPreference -DisableRealtimeMonitoring $false
```

**WARNING**: Only disable for testing! Re-enable immediately.

---

## ✅ Whitelist Instructions (Step-by-Step)

### **Windows Defender (Windows 10/11)**

1. Press `Win + I` to open Settings
2. Click "Privacy & Security"
3. Click "Windows Security"
4. Click "Virus & threat protection"
5. Under "Virus & threat protection settings", click "Manage settings"
6. Scroll down to "Exclusions"
7. Click "Add or remove exclusions"
8. Click "Add an exclusion" → "Folder"
9. Navigate to: `D:\Works\source\image-viewer`
10. Click "Select Folder"
11. Done! ✅

### **Verify Exclusion**:
```powershell
Get-MpPreference | Select-Object -ExpandProperty ExclusionPath
# Should include: D:\Works\source\image-viewer
```

---

## 🎯 Recommended Solution

### **Best Practice for Development**:

1. **Add folder exception** in antivirus (5 minutes)
2. **Use standard script**: `.\start-all-services.ps1`
3. **Enjoy silent mode** with background execution
4. **Keep antivirus enabled** for other protection

### **If Exception Not Possible**:

1. **Use safe mode**: `.\start-all-services-safe.ps1`
2. **Accept visible windows** (minor inconvenience)
3. **Or run manually** (4 terminals)

---

## 📝 Summary

**The scripts ARE SAFE** - this is a false positive.

**Evidence**:
✅ Open source (you can read every line)  
✅ No obfuscation  
✅ No network downloads  
✅ No registry modifications  
✅ No system file changes  
✅ Only starts .NET and Node.js processes  
✅ All operations are standard dev tasks  

**Recommendation**: Add antivirus exception for faster, cleaner development experience.

**Alternative**: Use `-Visible` flag or safe mode script if exceptions aren't allowed.

---

**Questions? Check SCRIPTS_README.md for full documentation!**

