namespace Subrom.Application.Interfaces;

/// <summary>
/// Service for watching file system changes in ROM directories.
/// Enables real-time detection of added/modified/deleted files.
/// </summary>
public interface IFileWatcherService : IDisposable {
	/// <summary>
	/// Starts watching a directory for changes.
	/// </summary>
	/// <param name="path">Directory path to watch.</param>
	/// <param name="includeSubdirectories">Whether to watch subdirectories.</param>
	/// <returns>Watcher ID for management.</returns>
	Guid StartWatching(string path, bool includeSubdirectories = true);

	/// <summary>
	/// Stops watching a specific directory.
	/// </summary>
	/// <param name="watcherId">Watcher ID returned from StartWatching.</param>
	void StopWatching(Guid watcherId);

	/// <summary>
	/// Stops all watchers.
	/// </summary>
	void StopAll();

	/// <summary>
	/// Gets all active watchers.
	/// </summary>
	IReadOnlyCollection<FileWatcherInfo> GetActiveWatchers();

	/// <summary>
	/// Gets pending changes that haven't been processed yet.
	/// </summary>
	IReadOnlyCollection<FileChangeEvent> GetPendingChanges();

	/// <summary>
	/// Clears pending changes after processing.
	/// </summary>
	void ClearPendingChanges();

	/// <summary>
	/// Pauses all watchers (useful during batch operations).
	/// </summary>
	void PauseAll();

	/// <summary>
	/// Resumes all paused watchers.
	/// </summary>
	void ResumeAll();

	/// <summary>
	/// Event raised when a file is created.
	/// </summary>
	event EventHandler<FileChangeEvent>? FileCreated;

	/// <summary>
	/// Event raised when a file is modified.
	/// </summary>
	event EventHandler<FileChangeEvent>? FileModified;

	/// <summary>
	/// Event raised when a file is deleted.
	/// </summary>
	event EventHandler<FileChangeEvent>? FileDeleted;

	/// <summary>
	/// Event raised when a file is renamed.
	/// </summary>
	event EventHandler<FileRenamedEvent>? FileRenamed;

	/// <summary>
	/// Event raised when there's an error watching a directory.
	/// </summary>
	event EventHandler<FileWatcherErrorEvent>? Error;
}

/// <summary>
/// Information about an active file watcher.
/// </summary>
public record FileWatcherInfo(
	Guid Id,
	string Path,
	bool IncludeSubdirectories,
	bool IsEnabled,
	DateTime StartedAt,
	long EventCount);

/// <summary>
/// A file change event.
/// </summary>
public record FileChangeEvent(
	Guid WatcherId,
	string FullPath,
	string FileName,
	FileWatcherChangeType ChangeType,
	DateTime Timestamp);

/// <summary>
/// A file renamed event.
/// </summary>
public record FileRenamedEvent(
	Guid WatcherId,
	string OldFullPath,
	string NewFullPath,
	string OldName,
	string NewName,
	DateTime Timestamp);

/// <summary>
/// A file watcher error event.
/// </summary>
public record FileWatcherErrorEvent(
	Guid WatcherId,
	string Path,
	string ErrorMessage,
	DateTime Timestamp);

/// <summary>
/// Types of file watcher changes.
/// </summary>
public enum FileWatcherChangeType {
	/// <summary>File was created.</summary>
	Created,
	/// <summary>File was modified.</summary>
	Modified,
	/// <summary>File was deleted.</summary>
	Deleted
}
