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
`t"path": "C:\\path\\to\\datfile.dat"
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
`t"regionPriority": ["USA", "Europe", "Japan", "World"],
`t"languagePriority": ["En", "De", "Fr", "Es"],
`t"preferParent": true,
`t"preferLatestRevision": true,
`t"preferVerified": true,
`t"excludeCategories": ["Beta", "Proto", "Sample", "Demo"]
}
```

**Response:**
```json
{
`t"datFileId": "guid",
`t"datFileName": "string",
`t"totalGames": 1000,
`t"filteredGames": 500,
`t"excludedGames": 500,
`t"options": { ... },
`t"games": [
`t{
`t  "gameName": "Super Mario Bros",
`t  "selectedGame": {
`t    "name": "Super Mario Bros (USA)",
`t    "region": "USA",
`t    "languages": "En",
`t    "score": 100
`t  },
`t  "alternatives": [...],
`t  "alternativeCount": 5
`t}
`t]
}
```

### Get Parent/Clone Analysis
```http
GET /api/dat/{id}/parent-clone?limit=200
```

**Response:**
```json
{
`t"datFileId": "guid",
`t"datFileName": "string",
`t"totalGames": 1000,
`t"parentCount": 400,
`t"cloneCount": 500,
`t"standaloneCount": 100,
`t"groups": [
`t{
`t  "parent": "Game Name",
`t  "cloneCount": 5,
`t  "clones": ["Clone 1", "Clone 2", ...]
`t}
`t]
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
`t"path": "C:\\ROMs",
`t"driveId": "guid (optional)",
`t"includeArchives": true,
`t"hashAlgorithms": ["Crc", "Md5", "Sha1"]
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
`t"totalGroups": 50,
`t"totalDuplicates": 100,
`t"wastedSpace": 1073741824,
`t"groups": [
`t{
`t  "count": 3,
`t  "totalSize": 1048576,
`t  "wastedSpace": 2097152,
`t  "crc": "12345678",
`t  "sha1": "abcdef...",
`t  "files": [
`t    { "fileName": "file1.rom", "path": "/path/to/file1", "size": 1048576 }
`t  ]
`t}
`t]
}
```

### Find Bad Dumps
```http
GET /api/romfiles/baddumps?driveId={guid}&limit=100
```

**Response:**
```json
{
`t"totalChecked": 1000,
`t"badDumpsFound": 5,
`t"suspectFiles": 10,
`t"results": [
`t{
`t  "file": { "fileName": "bad.rom", "path": "/path", "size": 1024 },
`t  "isBadDump": true,
`t  "status": "BadDump",
`t  "source": "No-Intro",
`t  "flags": "None",
`t  "datFileName": "Nintendo - NES.dat",
`t  "gameName": "Some Game",
`t  "romName": "some.rom"
`t}
`t]
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
`t"fileId": "guid",
`t"fileName": "game.rom",
`t"status": "Verified",
`t"matchedDatFiles": [...],
`t"matchedGames": [...],
`t"verifiedAt": "2026-01-21T12:00:00Z"
}
```

### Verify File by Path
```http
POST /api/verification/path
Content-Type: application/json

{
`t"path": "C:\\ROMs\\game.rom"
}
```

### Batch Verification
```http
POST /api/verification/batch
Content-Type: application/json

{
`t"fileIds": ["guid1", "guid2", "guid3"]
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
`t"totalFiles": 10000,
`t"verifiedFiles": 8000,
`t"unverifiedFiles": 1500,
`t"badDumps": 50,
`t"notInDat": 450
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
`t"folderTemplate": "{System}/{Region}/{Letter}",
`t"fileNameTemplate": "{Name}{Ext}"
}
```

### Preview Template
```http
POST /api/organization/templates/preview
Content-Type: application/json

{
`t"folderTemplate": "{System}/{Region}",
`t"fileNameTemplate": "{CleanName}{Ext}",
`t"sampleName": "Super Mario Bros (USA)",
`t"sampleSystem": "Nintendo - NES",
`t"sampleExtension": ".nes"
}
```

### Plan Organization (Dry Run)
```http
POST /api/organization/plan
Content-Type: application/json

{
`t"sourcePath": "C:\\ROMs\\Unsorted",
`t"destinationPath": "C:\\ROMs\\Sorted",
`t"templateId": "guid",
`t"move": false
}
```

### Execute Organization
```http
POST /api/organization/execute
Content-Type: application/json

{
`t"sourcePath": "C:\\ROMs\\Unsorted",
`t"destinationPath": "C:\\ROMs\\Sorted",
`t"templateId": "guid",
`t"move": true
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
`t"totalOperations": 100,
`t"successfulOperations": 95,
`t"failedOperations": 5,
`t"totalFilesProcessed": 50000,
`t"totalBytesProcessed": 107374182400,
`t"rolledBackOperations": 2,
`t"lastOperationAt": "2026-01-21T12:00:00Z"
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
`t"label": "My ROM Drive",
`t"path": "D:\\"
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
`t"message": "Error description",
`t"errors": ["Detail 1", "Detail 2"]
}
```

**HTTP Status Codes:**
- `200` - Success
- `400` - Bad Request
- `404` - Not Found
- `500` - Internal Server Error
