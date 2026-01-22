# Subrom API Reference

**Base URL:** `http://localhost:5000/api`

Last Updated: January 21, 2026

---

## Table of Contents

1. [DAT Files](#dat-files)
2. [Scans](#scans)
3. [ROM Files](#rom-files)
4. [Verification](#verification)
5. [Organization](#organization)
6. [Drives](#drives)

---

## DAT Files

### List DAT Files
```http
GET /api/dat
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `search` | string | Filter by name |
| `provider` | string | Filter by provider (NoIntro, TOSEC, Redump) |
| `skip` | int | Pagination offset |
| `take` | int | Page size (default: 50, max: 100) |

### Get DAT File Details
```http
GET /api/dat/{id}
```

### Import DAT File
```http
POST /api/dat/import
Content-Type: application/json

{
  "path": "C:\\path\\to\\datfile.dat"
}
```

### Toggle DAT Enabled Status
```http
POST /api/dat/{id}/toggle
```

### Apply 1G1R Filter
```http
POST /api/dat/{id}/1g1r
Content-Type: application/json

{
  "regionPriority": ["USA", "Europe", "Japan", "World"],
  "languagePriority": ["En", "De", "Fr", "Es"],
  "preferParent": true,
  "preferLatestRevision": true,
  "preferVerified": true,
  "excludeCategories": ["Beta", "Proto", "Sample", "Demo"]
}
```

**Response:**
```json
{
  "datFileId": "guid",
  "datFileName": "string",
  "totalGames": 1000,
  "filteredGames": 500,
  "excludedGames": 500,
  "options": { ... },
  "games": [
    {
      "gameName": "Super Mario Bros",
      "selectedGame": {
        "name": "Super Mario Bros (USA)",
        "region": "USA",
        "languages": "En",
        "score": 100
      },
      "alternatives": [...],
      "alternativeCount": 5
    }
  ]
}
```

### Get Parent/Clone Analysis
```http
GET /api/dat/{id}/parent-clone?limit=200
```

**Response:**
```json
{
  "datFileId": "guid",
  "datFileName": "string",
  "totalGames": 1000,
  "parentCount": 400,
  "cloneCount": 500,
  "standaloneCount": 100,
  "groups": [
    {
      "parent": "Game Name",
      "cloneCount": 5,
      "clones": ["Clone 1", "Clone 2", ...]
    }
  ]
}
```

### Lookup Game Parent/Clone
```http
GET /api/dat/{id}/parent-clone/{gameName}
```

---

## Scans

### List Scans
```http
GET /api/scans
```

### Get Scan Details
```http
GET /api/scans/{id}
```

### Start New Scan
```http
POST /api/scans
Content-Type: application/json

{
  "path": "C:\\ROMs",
  "driveId": "guid (optional)",
  "includeArchives": true,
  "hashAlgorithms": ["Crc", "Md5", "Sha1"]
}
```

**Note:** Scan progress is streamed via SignalR to `/hubs/progress`

### Cancel Scan
```http
DELETE /api/scans/{id}
```

---

## ROM Files

### List ROM Files
```http
GET /api/romfiles
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `driveId` | guid | Filter by drive |
| `status` | string | Filter by verification status |
| `search` | string | Search by name |
| `skip` | int | Pagination offset |
| `take` | int | Page size |

### Get ROM File Details
```http
GET /api/romfiles/{id}
```

### Find Duplicates
```http
GET /api/romfiles/duplicates?driveId={guid}&limit=100
```

**Response:**
```json
{
  "totalGroups": 50,
  "totalDuplicates": 100,
  "wastedSpace": 1073741824,
  "groups": [
    {
      "count": 3,
      "totalSize": 1048576,
      "wastedSpace": 2097152,
      "crc": "12345678",
      "sha1": "abcdef...",
      "files": [
        { "fileName": "file1.rom", "path": "/path/to/file1", "size": 1048576 }
      ]
    }
  ]
}
```

### Find Bad Dumps
```http
GET /api/romfiles/baddumps?driveId={guid}&limit=100
```

**Response:**
```json
{
  "totalChecked": 1000,
  "badDumpsFound": 5,
  "suspectFiles": 10,
  "results": [
    {
      "file": { "fileName": "bad.rom", "path": "/path", "size": 1024 },
      "isBadDump": true,
      "status": "BadDump",
      "source": "No-Intro",
      "flags": "None",
      "datFileName": "Nintendo - NES.dat",
      "gameName": "Some Game",
      "romName": "some.rom"
    }
  ]
}
```

### Check Single File for Bad Dump
```http
GET /api/romfiles/{id}/baddump
```

---

## Verification

### Verify Single File by ID
```http
POST /api/verification/file/{id}
```

**Response:**
```json
{
  "fileId": "guid",
  "fileName": "game.rom",
  "status": "Verified",
  "matchedDatFiles": [...],
  "matchedGames": [...],
  "verifiedAt": "2026-01-21T12:00:00Z"
}
```

### Verify File by Path
```http
POST /api/verification/path
Content-Type: application/json

{
  "path": "C:\\ROMs\\game.rom"
}
```

### Batch Verification
```http
POST /api/verification/batch
Content-Type: application/json

{
  "fileIds": ["guid1", "guid2", "guid3"]
}
```

### Verify All Files on Drive
```http
POST /api/verification/drive/{driveId}
```

### Get Verification Statistics
```http
GET /api/verification/stats
```

**Response:**
```json
{
  "totalFiles": 10000,
  "verifiedFiles": 8000,
  "unverifiedFiles": 1500,
  "badDumps": 50,
  "notInDat": 450
}
```

### Hash Lookup
```http
GET /api/verification/lookup?crc=12345678&md5=abc...&sha1=def...
```

---

## Organization

### Get Templates
```http
GET /api/organization/templates
```

### Validate Template
```http
POST /api/organization/templates/validate
Content-Type: application/json

{
  "folderTemplate": "{System}/{Region}/{Letter}",
  "fileNameTemplate": "{Name}{Ext}"
}
```

### Preview Template
```http
POST /api/organization/templates/preview
Content-Type: application/json

{
  "folderTemplate": "{System}/{Region}",
  "fileNameTemplate": "{CleanName}{Ext}",
  "sampleName": "Super Mario Bros (USA)",
  "sampleSystem": "Nintendo - NES",
  "sampleExtension": ".nes"
}
```

### Plan Organization (Dry Run)
```http
POST /api/organization/plan
Content-Type: application/json

{
  "sourcePath": "C:\\ROMs\\Unsorted",
  "destinationPath": "C:\\ROMs\\Sorted",
  "templateId": "guid",
  "move": false
}
```

### Execute Organization
```http
POST /api/organization/execute
Content-Type: application/json

{
  "sourcePath": "C:\\ROMs\\Unsorted",
  "destinationPath": "C:\\ROMs\\Sorted",
  "templateId": "guid",
  "move": true
}
```

### Rollback Operation
```http
POST /api/organization/{operationId}/rollback
```

### Get History
```http
GET /api/organization/history?limit=50
```

### Get Statistics
```http
GET /api/organization/stats
```

**Response:**
```json
{
  "totalOperations": 100,
  "successfulOperations": 95,
  "failedOperations": 5,
  "totalFilesProcessed": 50000,
  "totalBytesProcessed": 107374182400,
  "rolledBackOperations": 2,
  "lastOperationAt": "2026-01-21T12:00:00Z"
}
```

### Get Operation Details
```http
GET /api/organization/{operationId}
```

### Get Rollbackable Operations
```http
GET /api/organization/rollbackable
```

---

## Drives

### List Drives
```http
GET /api/drives
```

### Get Drive Details
```http
GET /api/drives/{id}
```

### Register Drive
```http
POST /api/drives
Content-Type: application/json

{
  "label": "My ROM Drive",
  "path": "D:\\"
}
```

### Refresh Drive Status
```http
POST /api/drives/{id}/refresh
```

---

## SignalR Hubs

### Progress Hub
**Endpoint:** `/hubs/progress`

**Client Events:**
- `ScanProgress` - Receives scan progress updates
- `HashProgress` - Receives hash computation progress
- `OrganizationProgress` - Receives organization progress

**Server Methods:**
- `SubscribeToScan(scanId)` - Subscribe to specific scan
- `UnsubscribeFromScan(scanId)` - Unsubscribe from scan

---

## Error Responses

All endpoints return standard error responses:

```json
{
  "message": "Error description",
  "errors": ["Detail 1", "Detail 2"]
}
```

**HTTP Status Codes:**
- `200` - Success
- `400` - Bad Request
- `404` - Not Found
- `500` - Internal Server Error
