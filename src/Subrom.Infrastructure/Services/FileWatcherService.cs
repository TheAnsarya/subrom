using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Subrom.Application.Interfaces;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Service for watching file system changes in ROM directories.
/// Uses FileSystemWatcher with debouncing to handle rapid changes.
/// </summary>
public sealed class FileWatcherService : IFileWatcherService {
	private readonly ILogger<FileWatcherService> _logger;
	private readonly ConcurrentDictionary<Guid, WatcherState> _watchers = new();
	private readonly ConcurrentQueue<FileChangeEvent> _pendingChanges = new();
	private readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(500);
	private bool _isPaused;
	private bool _disposed;

	// ROM file extensions to watch
	private static readonly HashSet<string> RomExtensions = new(StringComparer.OrdinalIgnoreCase) {
		".zip", ".7z", ".rar", ".gz", ".tar",
		".nes", ".sfc", ".smc", ".gba", ".gbc", ".gb", ".nds", ".3ds",
		".bin", ".cue", ".iso", ".chd", ".cso", ".pbp",
		".rom", ".a26", ".a52", ".a78", ".lnx",
		".md", ".smd", ".gen", ".32x", ".gg", ".sms",
		".pce", ".sgx", ".ngp", ".ngc",
		".z64", ".n64", ".v64", ".ndd",
		".wad", ".wbfs", ".gcm", ".dol",
		".xci", ".nsp"
	};

	public FileWatcherService(ILogger<FileWatcherService> logger) {
		_logger = logger;
	}

	public event EventHandler<FileChangeEvent>? FileCreated;
	public event EventHandler<FileChangeEvent>? FileModified;
	public event EventHandler<FileChangeEvent>? FileDeleted;
	public event EventHandler<FileRenamedEvent>? FileRenamed;
	public event EventHandler<FileWatcherErrorEvent>? Error;

	public Guid StartWatching(string path, bool includeSubdirectories = true) {
		ObjectDisposedException.ThrowIf(_disposed, this);
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		if (!Directory.Exists(path)) {
			throw new DirectoryNotFoundException($"Directory not found: {path}");
		}

		var id = Guid.NewGuid();
		var watcher = new FileSystemWatcher(path) {
			IncludeSubdirectories = includeSubdirectories,
			NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.DirectoryName,
			EnableRaisingEvents = !_isPaused
		};

		// Use debouncing to handle rapid changes
		var debounceTimers = new ConcurrentDictionary<string, Timer>();

		watcher.Created += (_, e) => HandleChange(id, e.FullPath, e.Name ?? "", FileWatcherChangeType.Created, debounceTimers);
		watcher.Changed += (_, e) => HandleChange(id, e.FullPath, e.Name ?? "", FileWatcherChangeType.Modified, debounceTimers);
		watcher.Deleted += (_, e) => HandleChange(id, e.FullPath, e.Name ?? "", FileWatcherChangeType.Deleted, debounceTimers);
		watcher.Renamed += (_, e) => HandleRenamed(id, e.OldFullPath, e.FullPath, e.OldName ?? "", e.Name ?? "");
		watcher.Error += (_, e) => HandleError(id, path, e.GetException());

		var state = new WatcherState(id, path, includeSubdirectories, watcher, debounceTimers);
		_watchers[id] = state;

		_logger.LogInformation("Started watching {Path} (ID: {Id}, Subdirs: {Subdirs})",
			path, id, includeSubdirectories);

		return id;
	}

	public void StopWatching(Guid watcherId) {
		if (_watchers.TryRemove(watcherId, out var state)) {
			state.Watcher.EnableRaisingEvents = false;
			state.Watcher.Dispose();

			foreach (var timer in state.DebounceTimers.Values) {
				timer.Dispose();
			}
			state.DebounceTimers.Clear();

			_logger.LogInformation("Stopped watching {Path} (ID: {Id})", state.Path, watcherId);
		}
	}

	public void StopAll() {
		foreach (var id in _watchers.Keys.ToList()) {
			StopWatching(id);
		}
	}

	public IReadOnlyCollection<FileWatcherInfo> GetActiveWatchers() {
		return _watchers.Values
			.Select(s => new FileWatcherInfo(
				s.Id,
				s.Path,
				s.IncludeSubdirectories,
				s.Watcher.EnableRaisingEvents,
				s.StartedAt,
				s.EventCount))
			.ToList();
	}

	public IReadOnlyCollection<FileChangeEvent> GetPendingChanges() {
		return _pendingChanges.ToArray();
	}

	public void ClearPendingChanges() {
		while (_pendingChanges.TryDequeue(out _)) { }
	}

	public void PauseAll() {
		_isPaused = true;
		foreach (var state in _watchers.Values) {
			state.Watcher.EnableRaisingEvents = false;
		}
		_logger.LogInformation("Paused all file watchers ({Count} watchers)", _watchers.Count);
	}

	public void ResumeAll() {
		_isPaused = false;
		foreach (var state in _watchers.Values) {
			state.Watcher.EnableRaisingEvents = true;
		}
		_logger.LogInformation("Resumed all file watchers ({Count} watchers)", _watchers.Count);
	}

	private void HandleChange(
		Guid watcherId,
		string fullPath,
		string fileName,
		FileWatcherChangeType changeType,
		ConcurrentDictionary<string, Timer> debounceTimers) {

		if (_isPaused) return;

		// Only watch ROM files
		var extension = Path.GetExtension(fullPath);
		if (!RomExtensions.Contains(extension)) return;

		// Debounce - cancel existing timer and start a new one
		var key = $"{fullPath}:{changeType}";

		if (debounceTimers.TryGetValue(key, out var existingTimer)) {
			existingTimer.Change(Timeout.Infinite, Timeout.Infinite);
			existingTimer.Dispose();
		}

		var timer = new Timer(_ => {
			if (debounceTimers.TryRemove(key, out var t)) {
				t.Dispose();
			}

			var evt = new FileChangeEvent(watcherId, fullPath, fileName, changeType, DateTime.UtcNow);
			_pendingChanges.Enqueue(evt);

			if (_watchers.TryGetValue(watcherId, out var state)) {
				Interlocked.Increment(ref state._eventCount);
			}

			switch (changeType) {
				case FileWatcherChangeType.Created:
					FileCreated?.Invoke(this, evt);
					break;
				case FileWatcherChangeType.Modified:
					FileModified?.Invoke(this, evt);
					break;
				case FileWatcherChangeType.Deleted:
					FileDeleted?.Invoke(this, evt);
					break;
			}

			_logger.LogDebug("File {ChangeType}: {Path}", changeType, fullPath);
		}, null, _debounceInterval, Timeout.InfiniteTimeSpan);

		debounceTimers[key] = timer;
	}

	private void HandleRenamed(Guid watcherId, string oldPath, string newPath, string oldName, string newName) {
		if (_isPaused) return;

		// Only watch ROM files
		var oldExt = Path.GetExtension(oldPath);
		var newExt = Path.GetExtension(newPath);
		if (!RomExtensions.Contains(oldExt) && !RomExtensions.Contains(newExt)) return;

		var evt = new FileRenamedEvent(watcherId, oldPath, newPath, oldName, newName, DateTime.UtcNow);

		if (_watchers.TryGetValue(watcherId, out var state)) {
			Interlocked.Increment(ref state._eventCount);
		}

		FileRenamed?.Invoke(this, evt);
		_logger.LogDebug("File renamed: {OldPath} → {NewPath}", oldPath, newPath);
	}

	private void HandleError(Guid watcherId, string path, Exception exception) {
		_logger.LogError(exception, "File watcher error for {Path}", path);
		Error?.Invoke(this, new FileWatcherErrorEvent(watcherId, path, exception.Message, DateTime.UtcNow));
	}

	public void Dispose() {
		if (_disposed) return;
		_disposed = true;

		StopAll();
	}

	private sealed class WatcherState {
		public Guid Id { get; }
		public string Path { get; }
		public bool IncludeSubdirectories { get; }
		public FileSystemWatcher Watcher { get; }
		public ConcurrentDictionary<string, Timer> DebounceTimers { get; }
		public DateTime StartedAt { get; }
		public long EventCount => _eventCount;

		internal long _eventCount;

		public WatcherState(
			Guid id,
			string path,
			bool includeSubdirectories,
			FileSystemWatcher watcher,
			ConcurrentDictionary<string, Timer> debounceTimers) {
			Id = id;
			Path = path;
			IncludeSubdirectories = includeSubdirectories;
			Watcher = watcher;
			DebounceTimers = debounceTimers;
			StartedAt = DateTime.UtcNow;
		}
	}
}
