using SharpCompress.Archives;
using SharpCompress.Common;
using Subrom.Application.Interfaces;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Archive service implementation using SharpCompress.
/// Supports ZIP, 7z, RAR, TAR, GZip, and other common archive formats.
/// </summary>
public sealed class SharpCompressArchiveService : IArchiveService {
	private static readonly HashSet<string> SupportedExtensionsSet = [
		".zip", ".7z", ".rar", ".tar", ".gz", ".tgz", ".bz2", ".xz", ".lz"
	];

	public IReadOnlySet<string> SupportedExtensions => SupportedExtensionsSet;

	public bool SupportsFormat(string extension) {
		ArgumentNullException.ThrowIfNull(extension);
		return SupportedExtensionsSet.Contains(extension.ToLowerInvariant());
	}

	public Task<IReadOnlyList<ArchiveEntry>> ListEntriesAsync(
		string archivePath,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);

		if (!File.Exists(archivePath)) {
			throw new FileNotFoundException("Archive not found", archivePath);
		}

		// SharpCompress operations are synchronous, wrap in Task.Run for async
		return Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();

			using var archive = ArchiveFactory.Open(archivePath);
			var entries = new List<ArchiveEntry>();

			foreach (var entry in archive.Entries) {
				cancellationToken.ThrowIfCancellationRequested();

				entries.Add(new ArchiveEntry {
					Path = NormalizePath(entry.Key ?? string.Empty),
					IsDirectory = entry.IsDirectory,
					UncompressedSize = entry.Size,
					CompressedSize = entry.CompressedSize,
					LastModified = entry.LastModifiedTime,
					Crc32 = entry.Crc > 0 ? (uint)entry.Crc : null
				});
			}

			return (IReadOnlyList<ArchiveEntry>)entries;
		}, cancellationToken);
	}

	public Task<Stream> OpenEntryAsync(
		string archivePath,
		string entryPath,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(entryPath);

		if (!File.Exists(archivePath)) {
			throw new FileNotFoundException("Archive not found", archivePath);
		}

		return Task.Run<Stream>(() => {
			cancellationToken.ThrowIfCancellationRequested();

			// We need to return a stream that keeps the archive open
			// Use a wrapper that manages the archive lifetime
			return new ArchiveEntryStream(archivePath, NormalizePath(entryPath));
		}, cancellationToken);
	}

	public async Task ExtractEntryAsync(
		string archivePath,
		string entryPath,
		string destinationPath,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(entryPath);
		ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

		if (!File.Exists(archivePath)) {
			throw new FileNotFoundException("Archive not found", archivePath);
		}

		var normalizedEntryPath = NormalizePath(entryPath);

		await Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();

			using var archive = ArchiveFactory.Open(archivePath);
			var entry = archive.Entries.FirstOrDefault(e =>
				string.Equals(NormalizePath(e.Key ?? string.Empty), normalizedEntryPath, StringComparison.OrdinalIgnoreCase));

			if (entry is null) {
				throw new FileNotFoundException($"Entry not found in archive: {entryPath}", entryPath);
			}

			if (entry.IsDirectory) {
				throw new InvalidOperationException($"Cannot extract directory entry: {entryPath}");
			}

			// Ensure destination directory exists
			var destDir = Path.GetDirectoryName(destinationPath);
			if (!string.IsNullOrEmpty(destDir)) {
				Directory.CreateDirectory(destDir);
			}

			entry.WriteToFile(destinationPath, new ExtractionOptions {
				ExtractFullPath = false,
				Overwrite = true
			});
		}, cancellationToken);
	}

	public async Task ExtractAllAsync(
		string archivePath,
		string destinationDirectory,
		IProgress<ExtractionProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
		ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

		if (!File.Exists(archivePath)) {
			throw new FileNotFoundException("Archive not found", archivePath);
		}

		Directory.CreateDirectory(destinationDirectory);

		await Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();

			using var archive = ArchiveFactory.Open(archivePath);
			var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();
			var totalEntries = entries.Count;
			var totalBytes = entries.Sum(e => e.Size);
			var processedEntries = 0;
			var processedBytes = 0L;

			foreach (var entry in entries) {
				cancellationToken.ThrowIfCancellationRequested();

				var entryPath = NormalizePath(entry.Key ?? string.Empty);
				var destPath = Path.Combine(destinationDirectory, entryPath);

				// Ensure destination directory exists
				var destDir = Path.GetDirectoryName(destPath);
				if (!string.IsNullOrEmpty(destDir)) {
					Directory.CreateDirectory(destDir);
				}

				entry.WriteToFile(destPath, new ExtractionOptions {
					ExtractFullPath = false,
					Overwrite = true
				});

				processedEntries++;
				processedBytes += entry.Size;

				progress?.Report(new ExtractionProgress {
					CurrentEntry = entryPath,
					ProcessedEntries = processedEntries,
					TotalEntries = totalEntries,
					ProcessedBytes = processedBytes,
					TotalBytes = totalBytes
				});
			}
		}, cancellationToken);
	}

	/// <summary>
	/// Normalizes archive entry paths to use forward slashes and remove leading separators.
	/// </summary>
	private static string NormalizePath(string path) {
		if (string.IsNullOrEmpty(path)) {
			return path;
		}

		// Normalize to forward slashes and trim leading/trailing slashes
		return path
			.Replace('\\', '/')
			.TrimStart('/')
			.TrimEnd('/');
	}

	/// <summary>
	/// A stream wrapper that keeps the archive open while the entry stream is being read.
	/// Disposes both the entry stream and archive when disposed.
	/// </summary>
	private sealed class ArchiveEntryStream : Stream {
		private readonly IArchive _archive;
		private readonly Stream _entryStream;
		private bool _disposed;

		public ArchiveEntryStream(string archivePath, string entryPath) {
			_archive = ArchiveFactory.Open(archivePath);

			var entry = _archive.Entries.FirstOrDefault(e =>
				string.Equals(NormalizePath(e.Key ?? string.Empty), entryPath, StringComparison.OrdinalIgnoreCase));

			if (entry is null) {
				_archive.Dispose();
				throw new FileNotFoundException($"Entry not found in archive: {entryPath}", entryPath);
			}

			if (entry.IsDirectory) {
				_archive.Dispose();
				throw new InvalidOperationException($"Cannot open directory entry as stream: {entryPath}");
			}

			_entryStream = entry.OpenEntryStream();
		}

		public override bool CanRead => _entryStream.CanRead;
		public override bool CanSeek => _entryStream.CanSeek;
		public override bool CanWrite => false;
		public override long Length => _entryStream.Length;

		public override long Position {
			get => _entryStream.Position;
			set => _entryStream.Position = value;
		}

		public override void Flush() => _entryStream.Flush();
		public override int Read(byte[] buffer, int offset, int count) => _entryStream.Read(buffer, offset, count);
		public override long Seek(long offset, SeekOrigin origin) => _entryStream.Seek(offset, origin);
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
			await _entryStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
			await _entryStream.ReadAsync(buffer, cancellationToken);

		protected override void Dispose(bool disposing) {
			if (!_disposed && disposing) {
				_entryStream.Dispose();
				_archive.Dispose();
				_disposed = true;
			}

			base.Dispose(disposing);
		}

		public override async ValueTask DisposeAsync() {
			if (!_disposed) {
				await _entryStream.DisposeAsync();
				_archive.Dispose();
				_disposed = true;
			}

			await base.DisposeAsync();
		}
	}
}
