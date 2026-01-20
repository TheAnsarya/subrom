using Microsoft.AspNetCore.Mvc;

using Subrom.Domain.Storage;
using Subrom.Services.Interfaces;

namespace Subrom.SubromAPI.Controllers;

/// <summary>
/// API controller for managing file scan operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ScanController(IScanService scanService) : ControllerBase {
	/// <summary>
	/// Starts a new scan job for the specified path.
	/// </summary>
	[HttpPost]
	[ProducesResponseType(typeof(ScanJob), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> StartScan([FromBody] StartScanRequest request) {
		if (string.IsNullOrWhiteSpace(request.Path)) {
			return BadRequest(new { error = "Path is required" });
		}

		if (!Directory.Exists(request.Path)) {
			return BadRequest(new { error = "Path does not exist" });
		}

		var job = await scanService.EnqueueScanAsync(
			request.Path,
			request.DriveId,
			request.Recursive,
			request.VerifyHashes
		);

		return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
	}

	/// <summary>
	/// Gets the status of a scan job.
	/// </summary>
	[HttpGet("{id:guid}")]
	[ProducesResponseType(typeof(ScanJob), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public IActionResult GetJob(Guid id) {
		var job = scanService.GetJob(id);
		return job is null ? NotFound() : Ok(job);
	}

	/// <summary>
	/// Gets all active scan jobs.
	/// </summary>
	[HttpGet]
	[ProducesResponseType(typeof(IEnumerable<ScanJob>), StatusCodes.Status200OK)]
	public IActionResult GetActiveJobs() {
		return Ok(scanService.GetActiveJobs());
	}

	/// <summary>
	/// Cancels a running scan job.
	/// </summary>
	[HttpDelete("{id:guid}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public IActionResult CancelJob(Guid id) {
		return scanService.CancelJob(id) ? NoContent() : NotFound();
	}
}

/// <summary>
/// Request to start a new scan job.
/// </summary>
public sealed record StartScanRequest {
	/// <summary>Path to scan for ROM files.</summary>
	public required string Path { get; init; }

	/// <summary>Optional drive ID if scanning a specific drive.</summary>
	public Guid? DriveId { get; init; }

	/// <summary>Whether to scan subdirectories recursively. Default is true.</summary>
	public bool Recursive { get; init; } = true;

	/// <summary>Whether to compute and verify file hashes. Default is true.</summary>
	public bool VerifyHashes { get; init; } = true;
}
