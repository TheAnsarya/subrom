using Subrom.Domain.Aggregates.Scanning;

namespace Subrom.Tests.Unit.Domain;

public class ScanJobResumabilityTests {
	#region Pause Tests

	[Fact]
	public void Pause_RunningJob_SetsPausedStatus() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full, targetPath: "/test/path");
		job.Start();

		// Act
		job.Pause("/test/path/lastfile.rom");

		// Assert
		Assert.Equal(ScanStatus.Paused, job.Status);
		Assert.NotNull(job.PausedAt);
		Assert.Equal("/test/path/lastfile.rom", job.LastProcessedPath);
	}

	[Fact]
	public void Pause_NotRunningJob_DoesNotChangeStatus() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full, targetPath: "/test/path");
		// Job is still Queued, not Running

		// Act
		job.Pause("/test/path/lastfile.rom");

		// Assert
		Assert.Equal(ScanStatus.Queued, job.Status);
		Assert.Null(job.PausedAt);
	}

	[Fact]
	public void Pause_EmitsPausedEvent() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full, targetPath: "/test/path");
		job.Start();
		job.SetPhase("Hashing", 100);
		job.ReportProgress("file50.rom", 50);

		// Act
		job.Pause("/test/path/file50.rom");

		// Assert
		var events = job.DomainEvents;
		var pausedEvent = events.OfType<ScanJobPausedEvent>().FirstOrDefault();
		Assert.NotNull(pausedEvent);
		Assert.Equal(job.Id, pausedEvent.JobId);
		Assert.Equal("/test/path/file50.rom", pausedEvent.LastProcessedPath);
		Assert.Equal(50, pausedEvent.ProcessedItems);
		Assert.Equal(100, pausedEvent.TotalItems);
	}

	#endregion

	#region Resume Tests

	[Fact]
	public void Resume_PausedJob_SetsRunningStatus() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full, targetPath: "/test/path");
		job.Start();
		job.Pause("/test/path/lastfile.rom");

		// Act
		job.Resume();

		// Assert
		Assert.Equal(ScanStatus.Running, job.Status);
		Assert.NotNull(job.ResumedAt);
		Assert.Equal(1, job.ResumeCount);
	}

	[Fact]
	public void Resume_FailedJob_SetsRunningStatus() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full, targetPath: "/test/path");
		job.Start();
		job.SetCheckpoint("/test/path/lastfile.rom", 50, 1024);
		job.Fail("Network error");

		// Act
		job.Resume();

		// Assert
		Assert.Equal(ScanStatus.Running, job.Status);
		Assert.Equal(1, job.ResumeCount);
		Assert.Equal("/test/path/lastfile.rom", job.LastProcessedPath);
	}

	[Fact]
	public void Resume_CompletedJob_DoesNotChangeStatus() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full, targetPath: "/test/path");
		job.Start();
		job.Complete();

		// Act
		job.Resume();

		// Assert
		Assert.Equal(ScanStatus.Completed, job.Status);
		Assert.Equal(0, job.ResumeCount);
	}

	[Fact]
	public void Resume_CancelledJob_DoesNotChangeStatus() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full, targetPath: "/test/path");
		job.Start();
		job.Cancel();

		// Act
		job.Resume();

		// Assert
		Assert.Equal(ScanStatus.Cancelled, job.Status);
		Assert.Equal(0, job.ResumeCount);
	}

	[Fact]
	public void Resume_MultipleResumes_IncrementsResumeCount() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full, targetPath: "/test/path");
		job.Start();
		job.Pause();

		// Act
		job.Resume();
		job.Pause();
		job.Resume();
		job.Pause();
		job.Resume();

		// Assert
		Assert.Equal(3, job.ResumeCount);
	}

	[Fact]
	public void Resume_EmitsResumedEvent() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full, targetPath: "/test/path");
		job.Start();
		job.Pause("/test/path/checkpoint.rom");

		// Act
		job.Resume();

		// Assert
		var events = job.DomainEvents;
		var resumedEvent = events.OfType<ScanJobResumedEvent>().FirstOrDefault();
		Assert.NotNull(resumedEvent);
		Assert.Equal(job.Id, resumedEvent.JobId);
		Assert.Equal(1, resumedEvent.ResumeCount);
		Assert.Equal("/test/path/checkpoint.rom", resumedEvent.ResumeFromPath);
	}

	#endregion

	#region CanResume Tests

	[Fact]
	public void CanResume_PausedJob_ReturnsTrue() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full);
		job.Start();
		job.Pause();

		// Assert
		Assert.True(job.CanResume);
	}

	[Fact]
	public void CanResume_FailedJob_ReturnsTrue() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full);
		job.Start();
		job.Fail("Error");

		// Assert
		Assert.True(job.CanResume);
	}

	[Theory]
	[InlineData(ScanStatus.Queued)]
	[InlineData(ScanStatus.Running)]
	[InlineData(ScanStatus.Completed)]
	[InlineData(ScanStatus.Cancelled)]
	public void CanResume_NonResumableStatus_ReturnsFalse(ScanStatus status) {
		// Arrange
		var job = ScanJob.Create(ScanType.Full);

		// Set the appropriate status
		if (status == ScanStatus.Running) job.Start();
		else if (status == ScanStatus.Completed) { job.Start(); job.Complete(); } else if (status == ScanStatus.Cancelled) { job.Start(); job.Cancel(); }
		// Queued is the default

		// Assert
		Assert.False(job.CanResume);
	}

	#endregion

	#region Checkpoint Tests

	[Fact]
	public void SetCheckpoint_UpdatesLastProcessedPath() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full);
		job.Start();

		// Act
		job.SetCheckpoint("/path/to/file100.rom", 100, 50000);

		// Assert
		Assert.Equal("/path/to/file100.rom", job.LastProcessedPath);
		Assert.Equal(100, job.ProcessedItems);
		Assert.Equal(50000, job.ProcessedBytes);
	}

	[Fact]
	public void SetCheckpoint_PreservesDataAfterPauseAndResume() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full, targetPath: "/scan/folder");
		job.Start();
		job.SetPhase("Hashing", 500, 1000000);
		job.SetCheckpoint("/scan/folder/game250.zip", 250, 500000);

		// Pause
		job.Pause();

		// Resume
		job.Resume();

		// Assert - checkpoint data preserved
		Assert.Equal("/scan/folder/game250.zip", job.LastProcessedPath);
		Assert.Equal(250, job.ProcessedItems);
		Assert.Equal(500000, job.ProcessedBytes);
		Assert.Equal(500, job.TotalItems);
		Assert.Equal("Hashing", job.CurrentPhase);
	}

	#endregion

	#region Progress After Resume Tests

	[Fact]
	public void ReportProgress_AfterResume_ContinuesFromCheckpoint() {
		// Arrange
		var job = ScanJob.Create(ScanType.Full);
		job.Start();
		job.SetPhase("Hashing", 100);
		job.ReportProgress("file50.rom", 50);
		job.Pause("/path/file50.rom");
		job.Resume();

		// Act - continue scanning from where we left off
		job.ReportProgress("file51.rom", 51);

		// Assert
		Assert.Equal(51, job.ProcessedItems);
		Assert.Equal("file51.rom", job.CurrentItem);
	}

	#endregion
}
