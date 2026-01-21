using Subrom.Application.Interfaces;
using Subrom.Domain.ValueObjects;

namespace Subrom.Infrastructure.Services;

/// <summary>
/// Service for detecting duplicate ROMs based on hash values.
/// Uses a combination of CRC32, MD5, and SHA1 for accurate matching.
/// </summary>
public sealed class DuplicateDetectionService : IDuplicateDetectionService {
	public Task<IReadOnlyList<DuplicateGroup>> FindDuplicatesAsync(
		IEnumerable<ScannedRomEntry> entries,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(entries);

		var groupedByHash = entries
			.GroupBy(e => e.Hashes)
			.Where(g => g.Count() > 1) // Only groups with duplicates
			.Select(g => {
				var list = g.ToList();
				return new DuplicateGroup(
					Hashes: g.Key,
					Entries: list,
					TotalSize: list.Sum(e => e.Size));
			})
			.OrderByDescending(g => g.WastedSpace)
			.ThenByDescending(g => g.Count)
			.ToList();

		return Task.FromResult<IReadOnlyList<DuplicateGroup>>(groupedByHash);
	}

	public Task<IReadOnlyList<ScannedRomEntry>> FindDuplicatesOfAsync(
		RomHashes targetHashes,
		IEnumerable<ScannedRomEntry> entries,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(entries);

		var matches = entries
			.Where(e => HashesMatch(e.Hashes, targetHashes))
			.ToList();

		return Task.FromResult<IReadOnlyList<ScannedRomEntry>>(matches);
	}

	public Task<IReadOnlyDictionary<RomHashes, IReadOnlyList<ScannedRomEntry>>> GroupByHashesAsync(
		IEnumerable<ScannedRomEntry> entries,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(entries);

		var grouped = entries
			.GroupBy(e => e.Hashes)
			.ToDictionary(
				g => g.Key,
				g => (IReadOnlyList<ScannedRomEntry>)g.ToList());

		return Task.FromResult<IReadOnlyDictionary<RomHashes, IReadOnlyList<ScannedRomEntry>>>(grouped);
	}

	/// <summary>
	/// Compares two hash sets for equality.
	/// Uses all available hashes for comparison.
	/// Two ROMs are considered duplicates if all their non-default hashes match.
	/// </summary>
	private static bool HashesMatch(RomHashes a, RomHashes b) {
		// If both have the same full hash set, they're duplicates
		// The RomHashes record struct provides value equality
		return a == b;
	}
}
