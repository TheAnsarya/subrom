using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Scanning;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Service for managing scan job resumability.
/// </summary>
public sealed class ScanResumeService : IScanResumeService {
	private readonly IScanJobRepository _scanJobRepository;
	private readonly IUnitOfWork _unitOfWork;

	public ScanResumeService(IScanJobRepository scanJobRepository, IUnitOfWork unitOfWork) {
		_scanJobRepository = scanJobRepository;
		_unitOfWork = unitOfWork;
	}

	/// <inheritdoc />
	public async Task<ScanJob?> PauseAsync(
		Guid jobId,
		string? lastProcessedPath = null,
		CancellationToken cancellationToken = default) {
		var job = await _scanJobRepository.GetByIdAsync(jobId, cancellationToken);
		if (job is null || job.Status != ScanStatus.Running) {
			return null;
		}

		job.Pause(lastProcessedPath);
		await _scanJobRepository.UpdateAsync(job, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return job;
	}

	/// <inheritdoc />
	public async Task<ScanJob?> ResumeAsync(Guid jobId, CancellationToken cancellationToken = default) {
		var job = await _scanJobRepository.GetByIdAsync(jobId, cancellationToken);
		if (job is null || !job.CanResume) {
			return null;
		}

		job.Resume();
		await _scanJobRepository.UpdateAsync(job, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);

		return job;
	}

	/// <inheritdoc />
	public async Task SetCheckpointAsync(
		Guid jobId,
		string lastProcessedPath,
		int processedItems = 0,
		long processedBytes = 0,
		CancellationToken cancellationToken = default) {
		var job = await _scanJobRepository.GetByIdAsync(jobId, cancellationToken);
		if (job is null || job.Status != ScanStatus.Running) {
			return;
		}

		job.SetCheckpoint(lastProcessedPath, processedItems, processedBytes);
		await _scanJobRepository.UpdateAsync(job, cancellationToken);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<ScanJob>> GetResumableJobsAsync(CancellationToken cancellationToken = default) =>
		await _scanJobRepository.GetResumableAsync(cancellationToken);

	/// <inheritdoc />
	public async Task<ScanJob?> GetResumableJobForPathAsync(string targetPath, CancellationToken cancellationToken = default) =>
		await _scanJobRepository.GetResumableForPathAsync(targetPath, cancellationToken);

	/// <inheritdoc />
	public async Task<ScanResumeInfo?> GetResumeInfoAsync(Guid jobId, CancellationToken cancellationToken = default) {
		var job = await _scanJobRepository.GetByIdAsync(jobId, cancellationToken);
		if (job is null) {
			return null;
		}

		return new ScanResumeInfo(
			job.Id,
			job.TargetPath,
			job.LastProcessedPath,
			job.ProcessedItems,
			job.TotalItems,
			job.ProcessedBytes,
			job.TotalBytes,
			job.ResumeCount,
			job.CurrentPhase);
	}
}
