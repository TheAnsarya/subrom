# Supported Archive Formats

Subrom can scan and verify ROMs inside compressed archives without extracting them first. This saves disk space and allows verification of archived ROM sets.

## Supported Formats

| Format | Extensions | Read | Extract | Notes |
|--------|------------|------|---------|-------|
| ZIP | `.zip` | ✅ | ✅ | Most common ROM archive format |
| 7-Zip | `.7z` | ✅ | ✅ | Popular for large ROM sets |
| RAR | `.rar` | ✅ | ❌ | Read-only, proprietary format |
| TAR | `.tar` | ✅ | ✅ | Unix tape archive |
| GZip | `.gz`, `.tgz` | ✅ | ✅ | Single-file compression |
| BZip2 | `.bz2` | ✅ | ✅ | Better compression than GZip |
| XZ | `.xz` | ✅ | ✅ | High compression ratio |
| LZip | `.lz` | ✅ | ✅ | LZMA-based compression |

## Archive Handling

### Scanning Archives

When scanning a folder, Subrom automatically:
1. Detects archive files by extension
2. Opens archives and lists contents
3. Computes hashes for each file inside
4. Matches against DAT file entries

No extraction to disk is required!

### Performance Considerations

| Format | Scan Speed | Memory Usage | Notes |
|--------|------------|--------------|-------|
| ZIP | ⚡ Fast | Low | Supports random access |
| 7z | 🐢 Slow | High | Solid archives require sequential read |
| RAR | 🐢 Slow | Medium | Depends on compression method |
| GZip | ⚡ Fast | Low | Single stream |

### Nested Archives

Subrom can scan archives within archives (e.g., a ZIP containing ZIPs), but this:
- Increases memory usage
- Slows down scanning
- Is limited to 2 levels of nesting

## ZIP Format Details

ZIP is the recommended format for ROM storage due to:
- Fast random access to individual files
- Good compression for most ROM types
- Universal support across platforms
- Per-file compression (can extract single ROMs)

### Compression Methods

Subrom supports all standard ZIP compression methods:
- Store (no compression)
- Deflate (most common)
- Deflate64
- BZip2
- LZMA

## 7-Zip Format Details

7-Zip offers better compression than ZIP but with tradeoffs:

**Pros:**
- 30-70% smaller than ZIP for some ROMs
- Supports very large archives (16 exabytes)
- Strong encryption option

**Cons:**
- Solid archives must be read sequentially
- Slower to scan than ZIP
- Higher memory requirements

### Solid vs Non-Solid Archives

| Type | Description | Scanning |
|------|-------------|----------|
| Solid | Files compressed together | Must decompress all to access any |
| Non-Solid | Files compressed separately | Can access individual files |

For ROM storage, consider using non-solid 7z archives (`7z a -ms=off`) for faster scanning.

## Best Practices

### Recommended Formats

1. **ZIP** - Best for general use
	 - Fast scanning
	 - Per-file access
	 - Universal compatibility

2. **7z (non-solid)** - Best for archival
	 - Better compression
	 - Still allows individual access
	 - Good for cold storage

### Not Recommended

- **RAR** - Proprietary, write support requires license
- **Solid 7z** - Too slow for regular scanning
- **Multi-part archives** - Not currently supported

### Compression Level vs Speed

| Use Case | Recommended Setting |
|----------|---------------------|
| Active collection | ZIP, normal compression |
| Long-term storage | 7z, maximum compression |
| Frequent scanning | ZIP, fast compression |

## Archive Metadata

When scanning archives, Subrom extracts:

| Metadata | Description |
|----------|-------------|
| File name | Entry path within archive |
| Uncompressed size | Original file size |
| Compressed size | Size in archive |
| CRC32 | Often stored in archive header |
| Last modified | File modification time |

Some archives store CRC32 in headers, allowing fast verification without full decompression.

## Troubleshooting

### "Archive format not supported"

- Check if the extension is in the supported list
- Some archives may use non-standard extensions
- Try renaming to the correct extension

### "Corrupt archive"

- Verify the archive opens in 7-Zip or similar
- Check for incomplete downloads
- Some very old archives may use obsolete methods

### Slow 7z Scanning

- The archive is likely solid
- Consider recompressing as non-solid
- Or use ZIP format for active collections
