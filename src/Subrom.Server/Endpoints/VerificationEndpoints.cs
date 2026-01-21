using Microsoft.EntityFrameworkCore;
using Subrom.Application.Services;
using Subrom.Domain.Aggregates.Storage;
using Subrom.Domain.ValueObjects;
using Subrom.Infrastructure.Persistence;

namespace Subrom.Server.Endpoints;

/// <summary>
/// ROM verification endpoints.
/// </summary>
public static class VerificationEndpoints {
	public static IEndpointRouteBuilder MapVerificationEndpoints(this IEndpointRouteBuilder endpoints) {
		var group = endpoints.MapGroup("/verification")
			.WithTags("Verification");

		// Verify a single ROM file by ID
		group.MapPost("/file/{id:guid}", async (
			Guid id,
			VerificationService verificationService,
			CancellationToken ct) => {
			try {
				var result = await verificationService.VerifyRomFileAsync(id, ct);
				return Results.Ok(result);
			} catch (KeyNotFoundException) {
				return Results.NotFound(new { Message = $"ROM file {id} not found" });
			} catch (InvalidOperationException ex) {
				return Results.BadRequest(new { Message = ex.Message });
			}
		});

		// Verify a file by path (ad-hoc, not in database)
		group.MapPost("/path", async (
			VerifyPathRequest request,
			VerificationService verificationService,
			CancellationToken ct) => {
			try {
				var result = await verificationService.VerifyFileAsync(request.FilePath, null, ct);
				return Results.Ok(result);
			} catch (FileNotFoundException) {
				return Results.NotFound(new { Message = $"File not found: {request.FilePath}" });
			}
		});

		// Batch verify multiple ROM files
		group.MapPost("/batch", async (
			BatchVerifyRequest request,
			VerificationService verificationService,
			CancellationToken ct) => {
			if (request.RomFileIds is null || request.RomFileIds.Count == 0) {
				return Results.BadRequest(new { Message = "At least one ROM file ID is required" });
			}

			var results = await verificationService.VerifyBatchAsync(request.RomFileIds, cancellationToken: ct);
			return Results.Ok(new {
				Total = results.Count,
				Verified = results.Count(r => r.IsMatch),
				Unverified = results.Count(r => !r.IsMatch && r.Error is null),
				Errors = results.Count(r => r.Error is not null),
				Results = results.Take(100) // Limit response size
			});
		});

		// Verify all unhashed files for a drive
		group.MapPost("/drive/{driveId:guid}", async (
			Guid driveId,
			bool? hashOnly,
			SubromDbContext db,
			VerificationService verificationService,
			CancellationToken ct) => {
			// Check drive exists
			var driveExists = await db.Drives.AnyAsync(d => d.Id == driveId, ct);
			if (!driveExists) {
				return Results.NotFound(new { Message = $"Drive {driveId} not found" });
			}

			// Get all verified files for this drive
			var query = db.RomFiles
				.Where(f => f.DriveId == driveId);

			// If hashOnly, only verify files with hashes but not yet verified
			if (hashOnly == true) {
				query = query.Where(f =>
					f.Crc != null && f.Md5 != null && f.Sha1 != null &&
					f.VerificationStatus == VerificationStatus.Unknown);
			} else {
				// All files that haven't been verified
				query = query.Where(f => f.VerificationStatus == VerificationStatus.Unknown);
			}

			var fileIds = await query
				.Select(f => f.Id)
				.Take(10000) // Safety limit
				.ToListAsync(ct);

			if (fileIds.Count == 0) {
				return Results.Ok(new { Message = "No files to verify", Total = 0 });
			}

			var results = await verificationService.VerifyBatchAsync(fileIds, cancellationToken: ct);

			return Results.Ok(new {
				Total = results.Count,
				Verified = results.Count(r => r.IsMatch),
				Unverified = results.Count(r => !r.IsMatch && r.Error is null),
				Errors = results.Count(r => r.Error is not null)
			});
		});

		// Get verification statistics
		group.MapGet("/stats", async (Guid? driveId, SubromDbContext db, CancellationToken ct) => {
			var query = db.RomFiles.AsNoTracking();

			if (driveId.HasValue) {
				query = query.Where(f => f.DriveId == driveId.Value);
			}

			var stats = await query
				.GroupBy(f => f.VerificationStatus)
				.Select(g => new { Status = g.Key, Count = g.Count() })
				.ToListAsync(ct);

			var total = stats.Sum(s => s.Count);
			var verified = stats.FirstOrDefault(s => s.Status == VerificationStatus.Verified)?.Count ?? 0;
			var unverified = stats.FirstOrDefault(s => s.Status == VerificationStatus.Unknown)?.Count ?? 0;
			var notInDat = stats.FirstOrDefault(s => s.Status == VerificationStatus.NotInDat)?.Count ?? 0;
			var badDump = stats.FirstOrDefault(s => s.Status == VerificationStatus.BadDump)?.Count ?? 0;

			// Count files with hashes
			var hashedCount = await (driveId.HasValue
				? db.RomFiles.Where(f => f.DriveId == driveId.Value)
				: db.RomFiles)
				.CountAsync(f => f.Crc != null && f.Md5 != null && f.Sha1 != null, ct);

			return Results.Ok(new {
				Total = total,
				Verified = verified,
				VerifiedPercentage = total > 0 ? Math.Round((double)verified / total * 100, 1) : 0,
				Unverified = unverified,
				NotInDat = notInDat,
				BadDump = badDump,
				HasHashes = hashedCount,
				NeedsHashing = total - hashedCount
			});
		});

		// Lookup ROM by hash
		group.MapGet("/lookup", async (
			string? crc,
			string? md5,
			string? sha1,
			VerificationService verificationService,
			CancellationToken ct) => {
			if (crc is null && md5 is null && sha1 is null) {
				return Results.BadRequest(new { Message = "At least one hash must be provided (crc, md5, or sha1)" });
			}

			// Build a partial RomHashes with available values
			var crcVal = Crc.TryCreate(crc ?? "", out var c) ? c.Value : default;
			var md5Val = Md5.TryCreate(md5 ?? "", out var m) ? m.Value : default;
			var sha1Val = Sha1.TryCreate(sha1 ?? "", out var s) ? s.Value : default;

			var hashes = new RomHashes(crcVal, md5Val, sha1Val);
			var match = await verificationService.LookupByHashesAsync(hashes, ct);

			if (match is null) {
				return Results.Ok(new { Count = 0, Matches = Array.Empty<object>() });
			}

			return Results.Ok(new {
				Count = 1,
				Matches = new[] { match }
			});
		});

		return endpoints;
	}
}

public record VerifyPathRequest(string FilePath);

public record BatchVerifyRequest(List<Guid>? RomFileIds);
