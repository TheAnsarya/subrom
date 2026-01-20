using Microsoft.AspNetCore.SignalR;

using Subrom.Domain.Storage;

namespace Subrom.SubromAPI.Hubs;

/// <summary>
/// SignalR hub for real-time scan progress updates.
/// </summary>
public sealed class ScanHub : Hub {
	/// <summary>
	/// Adds the connection to a scan job's group to receive progress updates.
	/// </summary>
	/// <param name="jobId">The scan job ID to subscribe to.</param>
	public async Task SubscribeToJob(Guid jobId) {
		await Groups.AddToGroupAsync(Context.ConnectionId, $"scan-{jobId}");
	}

	/// <summary>
	/// Removes the connection from a scan job's group.
	/// </summary>
	/// <param name="jobId">The scan job ID to unsubscribe from.</param>
	public async Task UnsubscribeFromJob(Guid jobId) {
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"scan-{jobId}");
	}

	/// <summary>
	/// Subscribes to all scan job updates.
	/// </summary>
	public async Task SubscribeToAllScans() {
		await Groups.AddToGroupAsync(Context.ConnectionId, "all-scans");
	}

	/// <summary>
	/// Unsubscribes from all scan job updates.
	/// </summary>
	public async Task UnsubscribeFromAllScans() {
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all-scans");
	}
}

/// <summary>
/// Interface for sending scan progress updates to connected clients.
/// </summary>
public interface IScanHubClient {
	/// <summary>Notifies clients of scan progress.</summary>
	Task ScanProgress(ScanProgressUpdate update);

	/// <summary>Notifies clients that a scan has started.</summary>
	Task ScanStarted(ScanJob job);

	/// <summary>Notifies clients that a scan has completed.</summary>
	Task ScanCompleted(ScanJob job);

	/// <summary>Notifies clients that a scan has failed.</summary>
	Task ScanFailed(ScanJob job);

	/// <summary>Notifies clients that a scan was cancelled.</summary>
	Task ScanCancelled(ScanJob job);
}

/// <summary>
/// Progress update message sent via SignalR.
/// </summary>
public sealed record ScanProgressUpdate {
	/// <summary>The scan job ID.</summary>
	public required Guid JobId { get; init; }

	/// <summary>Progress percentage (0-100).</summary>
	public required double Progress { get; init; }

	/// <summary>Total files to process.</summary>
	public required int TotalFiles { get; init; }

	/// <summary>Files processed so far.</summary>
	public required int ProcessedFiles { get; init; }

	/// <summary>Files that matched a DAT entry.</summary>
	public int VerifiedFiles { get; init; }

	/// <summary>Files that didn't match any DAT entry.</summary>
	public int UnknownFiles { get; init; }

	/// <summary>Files with errors.</summary>
	public int ErrorFiles { get; init; }

	/// <summary>Current file being processed.</summary>
	public string? CurrentFile { get; init; }

	/// <summary>Creates an update from a ScanJob.</summary>
	public static ScanProgressUpdate FromJob(ScanJob job) => new() {
		JobId = job.Id,
		Progress = job.Progress,
		TotalFiles = job.TotalFiles,
		ProcessedFiles = job.ProcessedFiles,
		VerifiedFiles = job.VerifiedFiles,
		UnknownFiles = job.UnknownFiles,
		ErrorFiles = job.ErrorFiles,
		CurrentFile = job.CurrentFile,
	};
}
