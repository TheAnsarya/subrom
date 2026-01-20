# Subrom API Design

## Overview

The Subrom API is a RESTful JSON API that provides all functionality for the ROM management system. It's designed to be consumed by the React frontend and potentially third-party tools.

## Base URL

```
http://localhost:5000/api/v1
```

## Authentication

For local use, authentication is optional. When enabled:

```http
Authorization: Bearer <token>
```

## Common Response Format

### Success Response
```json
{
	"success": true,
	"data": { ... },
	"meta": {
		"timestamp": "2026-01-19T10:30:00Z",
		"requestId": "abc123"
	}
}
```

### Error Response
```json
{
	"success": false,
	"error": {
		"code": "DAT_NOT_FOUND",
		"message": "The requested DAT file was not found",
		"details": { ... }
	},
	"meta": {
		"timestamp": "2026-01-19T10:30:00Z",
		"requestId": "abc123"
	}
}
```

## Pagination

```json
{
	"data": [...],
	"pagination": {
		"page": 1,
		"pageSize": 50,
		"totalItems": 2847,
		"totalPages": 57
	}
}
```

---

## Endpoints

### DAT Files

#### List DAT Files
```http
GET /dats
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `provider` | string | Filter by provider (no-intro, tosec, redump, good) |
| `system` | string | Filter by system |
| `search` | string | Search in name/description |
| `page` | int | Page number (default: 1) |
| `pageSize` | int | Items per page (default: 50) |

**Response:**
```json
{
	"data": [
		{
			"id": "550e8400-e29b-41d4-a716-446655440000",
			"name": "Nintendo - Nintendo Entertainment System",
			"provider": "no-intro",
			"version": "2026-01-15",
			"games": 2847,
			"roms": 3456,
			"importedAt": "2026-01-16T10:30:00Z",
			"lastChecked": "2026-01-19T08:00:00Z"
		}
	],
	"pagination": { ... }
}
```

#### Get DAT File
```http
GET /dats/{id}
```

**Response:**
```json
{
	"data": {
		"id": "550e8400-e29b-41d4-a716-446655440000",
		"name": "Nintendo - Nintendo Entertainment System",
		"description": "No-Intro NES DAT",
		"provider": "no-intro",
		"version": "2026-01-15",
		"author": "No-Intro",
		"url": "https://datomatic.no-intro.org",
		"games": 2847,
		"roms": 3456,
		"importedAt": "2026-01-16T10:30:00Z",
		"filePath": "C:\\Subrom\\dats\\no-intro\\nes.dat",
		"header": { ... }
	}
}
```

#### Import DAT File
```http
POST /dats/import
Content-Type: multipart/form-data
```

**Request Body:**
- `file`: DAT file (multipart)
- `provider`: Provider name (optional)

**Response:**
```json
{
	"data": {
		"id": "550e8400-e29b-41d4-a716-446655440000",
		"name": "Nintendo - Nintendo Entertainment System",
		"gamesImported": 2847,
		"romsImported": 3456
	}
}
```

#### Update DATs from Providers
```http
POST /dats/update
```

**Request Body:**
```json
{
	"providers": ["no-intro", "tosec"],
	"systems": ["nes", "snes"]
}
```

**Response:**
```json
{
	"data": {
		"jobId": "job-123",
		"status": "started"
	}
}
```

#### Delete DAT File
```http
DELETE /dats/{id}
```

---

### Games

#### List Games in DAT
```http
GET /dats/{datId}/games
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `search` | string | Search in name/description |
| `status` | string | Filter by status (have, missing, partial) |
| `region` | string | Filter by region |
| `page` | int | Page number |
| `pageSize` | int | Items per page |

**Response:**
```json
{
	"data": [
		{
			"id": "game-123",
			"name": "Super Mario Bros. (USA)",
			"description": "Super Mario Bros.",
			"year": "1985",
			"manufacturer": "Nintendo",
			"status": "have",
			"roms": [
				{
					"name": "Super Mario Bros. (USA).nes",
					"size": 40976,
					"crc32": "d445f698",
					"md5": "811b027eaf99c2def7b933c5208636de",
					"sha1": "facee9c577a5262dbee256de7740d2d87e85f3e0",
					"status": "verified"
				}
			]
		}
	],
	"pagination": { ... }
}
```

---

### Drives

#### List Drives
```http
GET /drives
```

**Response:**
```json
{
	"data": [
		{
			"id": "drive-123",
			"label": "ROM Storage",
			"path": "D:\\ROMs",
			"volumeId": "1234-5678",
			"totalSpace": 2000000000000,
			"freeSpace": 1500000000000,
			"romCount": 12345,
			"isOnline": true,
			"lastSeen": "2026-01-19T10:00:00Z",
			"lastScanned": "2026-01-19T08:00:00Z"
		}
	]
}
```

#### Register Drive
```http
POST /drives
```

**Request Body:**
```json
{
	"path": "D:\\ROMs",
	"label": "ROM Storage",
	"scanOnAdd": true
}
```

#### Update Drive
```http
PUT /drives/{id}
```

**Request Body:**
```json
{
	"label": "New Label",
	"enabled": true
}
```

#### Remove Drive
```http
DELETE /drives/{id}
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `keepRecords` | bool | Keep ROM records (default: true) |

---

### Scanning

#### Start Scan
```http
POST /scan
```

**Request Body:**
```json
{
	"driveIds": ["drive-123"],
	"paths": ["D:\\ROMs\\NES"],
	"options": {
		"recursive": true,
		"verifyHashes": true,
		"includeArchives": true,
		"skipExisting": false
	}
}
```

**Response:**
```json
{
	"data": {
		"jobId": "scan-456",
		"status": "started",
		"estimatedFiles": 15000
	}
}
```

#### Get Scan Status
```http
GET /scan/{jobId}
```

**Response:**
```json
{
	"data": {
		"jobId": "scan-456",
		"status": "running",
		"startedAt": "2026-01-19T10:00:00Z",
		"progress": {
			"totalFiles": 15000,
			"processedFiles": 7500,
			"percentage": 50,
			"currentFile": "D:\\ROMs\\NES\\game.nes",
			"filesPerSecond": 45
		},
		"results": {
			"verified": 6500,
			"badDump": 45,
			"unknown": 900,
			"skipped": 55
		}
	}
}
```

#### Get Scan Results
```http
GET /scan/{jobId}/results
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `status` | string | Filter by result status |
| `page` | int | Page number |
| `pageSize` | int | Items per page |

**Response:**
```json
{
	"data": [
		{
			"filePath": "D:\\ROMs\\NES\\Super Mario Bros.nes",
			"size": 40976,
			"hashes": {
				"crc32": "d445f698",
				"md5": "811b027eaf99c2def7b933c5208636de",
				"sha1": "facee9c577a5262dbee256de7740d2d87e85f3e0"
			},
			"matches": [
				{
					"datId": "dat-123",
					"gameId": "game-456",
					"romName": "Super Mario Bros. (USA).nes",
					"matchType": "exact"
				}
			],
			"status": "verified"
		}
	],
	"pagination": { ... }
}
```

#### Cancel Scan
```http
POST /scan/{jobId}/cancel
```

---

### ROMs

#### List ROMs
```http
GET /roms
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `driveId` | string | Filter by drive |
| `datId` | string | Filter by DAT |
| `status` | string | Filter by status |
| `search` | string | Search in path/name |
| `page` | int | Page number |
| `pageSize` | int | Items per page |

**Response:**
```json
{
	"data": [
		{
			"id": "rom-123",
			"filePath": "D:\\ROMs\\NES\\Super Mario Bros.nes",
			"driveId": "drive-456",
			"size": 40976,
			"hashes": {
				"crc32": "d445f698",
				"md5": "811b027eaf99c2def7b933c5208636de",
				"sha1": "facee9c577a5262dbee256de7740d2d87e85f3e0"
			},
			"verifiedAt": "2026-01-19T08:00:00Z",
			"isOnline": true,
			"datMatches": [
				{
					"datId": "dat-123",
					"datName": "No-Intro NES",
					"gameName": "Super Mario Bros. (USA)"
				}
			]
		}
	],
	"pagination": { ... }
}
```

#### Get Missing ROMs
```http
GET /roms/missing
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `datId` | string | Filter by DAT |
| `system` | string | Filter by system |
| `page` | int | Page number |
| `pageSize` | int | Items per page |

#### Get Duplicate ROMs
```http
GET /roms/duplicates
```

---

### Organization

#### Preview Organization
```http
POST /organize/preview
```

**Request Body:**
```json
{
	"sourceIds": ["rom-123", "rom-456"],
	"targetPath": "D:\\Organized",
	"rules": {
		"template": "{System}/{Region}/{Game}.{Extension}",
		"preferRegion": ["USA", "Europe", "Japan"],
		"use1G1R": true,
		"handleDuplicates": "skip"
	}
}
```

**Response:**
```json
{
	"data": {
		"previewId": "preview-789",
		"operations": [
			{
				"type": "move",
				"source": "D:\\ROMs\\game.nes",
				"destination": "D:\\Organized\\NES\\USA\\Super Mario Bros.nes"
			}
		],
		"summary": {
			"moves": 150,
			"renames": 45,
			"skips": 10,
			"conflicts": 2
		}
	}
}
```

#### Execute Organization
```http
POST /organize/execute
```

**Request Body:**
```json
{
	"previewId": "preview-789"
}
```

#### Undo Organization
```http
POST /organize/undo
```

**Request Body:**
```json
{
	"operationId": "op-123"
}
```

---

### Statistics

#### Get Collection Statistics
```http
GET /stats
```

**Response:**
```json
{
	"data": {
		"totalRoms": 45231,
		"totalSize": 150000000000,
		"datsCoverage": {
			"complete": 12,
			"partial": 8,
			"empty": 3
		},
		"bySystem": [
			{
				"system": "NES",
				"total": 2847,
				"have": 2500,
				"missing": 347,
				"percentage": 87.8
			}
		],
		"byProvider": [
			{
				"provider": "no-intro",
				"dats": 25,
				"games": 50000,
				"coverage": 85.5
			}
		],
		"driveStats": {
			"totalDrives": 4,
			"onlineDrives": 3,
			"totalCapacity": 10000000000000,
			"usedCapacity": 3500000000000
		}
	}
}
```

---

### Configuration

#### Get Settings
```http
GET /settings
```

#### Update Settings
```http
PUT /settings
```

**Request Body:**
```json
{
	"scanning": {
		"parallelThreads": 4,
		"skipHiddenFiles": true,
		"supportedExtensions": [".zip", ".7z", ".nes"]
	},
	"organization": {
		"defaultTemplate": "{System}/{Game}.{Extension}",
		"preferRegion": ["USA", "Europe", "Japan"]
	},
	"providers": {
		"noIntro": {
			"enabled": true,
			"updateInterval": "7d"
		}
	}
}
```

---

## WebSocket Events

For real-time updates, connect to:
```
ws://localhost:5000/ws
```

### Event Types

#### Scan Progress
```json
{
	"type": "scan.progress",
	"data": {
		"jobId": "scan-456",
		"processed": 7500,
		"total": 15000,
		"currentFile": "game.nes"
	}
}
```

#### Scan Complete
```json
{
	"type": "scan.complete",
	"data": {
		"jobId": "scan-456",
		"verified": 14000,
		"duration": 300
	}
}
```

#### Drive Status Change
```json
{
	"type": "drive.statusChange",
	"data": {
		"driveId": "drive-123",
		"isOnline": false
	}
}
```

#### DAT Update Available
```json
{
	"type": "dat.updateAvailable",
	"data": {
		"datId": "dat-123",
		"currentVersion": "2026-01-01",
		"newVersion": "2026-01-15"
	}
}
```

---

## Error Codes

| Code | Description |
|------|-------------|
| `DAT_NOT_FOUND` | DAT file not found |
| `DRIVE_NOT_FOUND` | Drive not found |
| `DRIVE_OFFLINE` | Drive is offline |
| `SCAN_IN_PROGRESS` | A scan is already running |
| `INVALID_PATH` | Invalid file path |
| `PARSE_ERROR` | Failed to parse DAT file |
| `HASH_MISMATCH` | Hash verification failed |
| `PERMISSION_DENIED` | File permission denied |
| `DISK_FULL` | Insufficient disk space |

---

## Rate Limiting

For local use, no rate limiting is applied. When exposed externally:
- 100 requests per minute for normal endpoints
- 10 requests per minute for scan/organize operations

---

## Related Documents

- [Project Roadmap](roadmap.md)
- [Architecture Overview](architecture.md)
- [UI Design Plans](ui-plans.md)
