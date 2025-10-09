# Root Folder Organization Plan

## Current Issues
- 21 PowerShell scripts in root
- 11 markdown files in root (should be in docs/)
- 10 deployment files scattered
- 2 temp extract folders
- Multiple environment files with inconsistent naming
- Legacy `_outdated` folder (old Node.js implementation)

## Proposed Structure

```
image-viewer/
├── src/                          # Source code (already organized)
├── docs/                         # All documentation
├── scripts/                      # All operational scripts
│   ├── deployment/              # Deployment scripts
│   ├── development/             # Development/testing scripts
│   ├── maintenance/             # Cleanup/maintenance scripts
│   └── monitoring/              # Monitoring scripts
├── deployment/                   # Deployment configurations
│   ├── docker/                  # Docker files
│   ├── docker-compose/          # Docker compose files
│   └── nginx/                   # Nginx configs
├── config/                       # Configuration files
│   ├── appsettings.*.json
│   └── env files
├── .github/                      # GitHub specific (if needed)
├── monitoring/                   # Monitoring configs (Prometheus, etc.)
├── _archive/                     # Archived/outdated code
│   └── _outdated/               # Old Node.js implementation
└── [root files]                  # Only essential files
    ├── .gitignore
    ├── README.md
    ├── ImageViewer.sln (symlink or reference)
    ├── docker-compose.yml (main)
    └── LICENSE
```

## Organization Actions

### 1. Move PowerShell Scripts
```
Root → scripts/
  Development:
    - test-*.ps1 → scripts/development/
    - run-test.ps1 → scripts/development/
    - manual-thumbnail-trigger.ps1 → scripts/development/
    - queue-all-thumbnails.ps1 → scripts/development/
    - setup-aiasag.ps1 → scripts/development/
    
  Deployment:
    - start-api.ps1 → scripts/deployment/
    - stop-api*.ps1 → scripts/deployment/
    - stop-services.ps1 → scripts/deployment/
    - deploy-*.ps1 → scripts/deployment/
    
  Maintenance:
    - clear-*.ps1 → scripts/maintenance/
    - check-*.ps1 → scripts/maintenance/
```

### 2. Move Documentation
```
Root → docs/
  - BUGS_FOUND_AND_FIXED.md → docs/09-troubleshooting/
  - DEEP_CODE_REVIEW_REPORT.md → docs/08-source-code-review/
  - DOCUMENTATION_REVIEW_COMPLETE.md → docs/
  - ISSUE_ANALYSIS.md → docs/09-troubleshooting/
  - LEGACY_CODE_REVIEW_REPORT.md → docs/08-source-code-review/
  - PROJECT_COMPLETION_SUMMARY.md → docs/
  - REFACTORING_*.md → docs/07-migration/
  - DEPLOY_README.md → docs/05-deployment/
  - README-Windows-Deployment.md → docs/05-deployment/
```

### 3. Organize Deployment Files
```
Root → deployment/
  docker/:
    - Dockerfile
    - Dockerfile.Worker
    - .dockerignore
  
  docker-compose/:
    - docker-compose.yml (main - keep in root as symlink)
    - docker-compose.windows.yml
    - docker-compose.override.yml
  
  scripts/:
    - deploy.bat
    - deploy-docker.sh
```

### 4. Organize Config Files
```
Root → config/
  - appsettings.Local.json
  - env.development
  - env.example
  - env production → env.production
  - env staging → env.staging
  - bulk-test.json
```

### 5. Archive Old Code
```
Root/_outdated → _archive/nodejs-legacy/
  - Entire _outdated folder
```

### 6. Clean Up Temp Folders
```
Delete (after confirming no important data):
  - temp_extract/
  - temp_extract2/
```

### 7. Keep in Root (Essential Only)
- README.md (main project readme)
- .gitignore
- docker-compose.yml (primary compose file)
- LICENSE (if exists in _outdated)
- src/ (source code)
- docs/ (documentation)
- scripts/ (operational scripts)
- deployment/ (deployment configs)
- config/ (configuration files)
- monitoring/ (already organized)
- _archive/ (archived code)

## Implementation Order

1. **Create new folders** (safe operation)
2. **Move documentation** (no code changes)
3. **Move scripts** (test after moving)
4. **Move deployment files** (critical - test carefully)
5. **Move config files** (update paths in scripts)
6. **Archive old code** (after confirming not in use)
7. **Delete temp folders** (after backup if needed)
8. **Update .gitignore** (exclude temp folders)
9. **Update README.md** (new folder structure)
10. **Commit changes** (with detailed message)

## Files to Update After Organization

1. **Scripts that reference paths:**
   - All scripts in scripts/ that reference config files
   - Deployment scripts that reference Dockerfiles
   - Monitoring scripts

2. **Docker Compose files:**
   - Update volume mounts if needed
   - Update build contexts

3. **Documentation:**
   - Update README.md with new structure
   - Update deployment docs with new paths

4. **.gitignore:**
   - Add temp_extract*
   - Add any new ignore patterns

## Verification Checklist

- [ ] All scripts still work after moving
- [ ] Docker builds successfully
- [ ] API starts correctly
- [ ] Worker starts correctly
- [ ] Deployment scripts work
- [ ] No broken path references
- [ ] Git history preserved
- [ ] Documentation updated

