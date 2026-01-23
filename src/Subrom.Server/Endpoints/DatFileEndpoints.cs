using Microsoft.EntityFrameworkCore;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Organization;
using Subrom.Infrastructure.Persistence;

namespace Subrom.Server.Endpoints;

/// <summary>
/// DAT file management endpoints.
/// </summary>
public static class DatFileEndpoints {
	public static IEndpointRouteBuilder MapDatFileEndpoints(this IEndpointRouteBuilder endpoints) {
		var group = endpoints.MapGroup("/datfiles")
			.WithTags("DAT Files");

		// Get all DAT files (summary)
		group.MapGet("/", async (SubromDbContext db, CancellationToken ct) => {
			var datFiles = await db.DatFiles
				.AsNoTracking()
				.OrderBy(d => d.CategoryPath)
				.ThenBy(d => d.Name)
				.Select(d => new {
					d.Id,
					d.FileName,
					d.Name,
					d.Description,
					d.Version,
					d.Provider,
					d.System,
					d.CategoryPath,
					d.GameCount,
					d.RomCount,
					d.TotalSize,
					d.IsEnabled,
					d.ImportedAt
				})
				.ToListAsync(ct);

			return Results.Ok(datFiles);
		});

		// Get DAT file details
		group.MapGet("/{id:guid}", async (Guid id, SubromDbContext db, CancellationToken ct) => {
			var datFile = await db.DatFiles
				.AsNoTracking()
				.FirstOrDefaultAsync(d => d.Id == id, ct);

			return datFile is null
				? Results.NotFound()
				: Results.Ok(datFile);
		});

		// Get games in a DAT file (paginated)
		group.MapGet("/{id:guid}/games", async (
			Guid id,
			int page,
			int pageSize,
			string? search,
			SubromDbContext db,
			CancellationToken ct) => {
				pageSize = Math.Clamp(pageSize, 1, 1000);
				page = Math.Max(0, page);

				var query = db.Games
					.AsNoTracking()
					.Where(g => g.DatFileId == id);

				if (!string.IsNullOrWhiteSpace(search)) {
					query = query.Where(g =>
						g.Name.Contains(search) ||
						(g.Description != null && g.Description.Contains(search)));
				}

				var total = await query.CountAsync(ct);
				var games = await query
					.OrderBy(g => g.Name)
					.Skip(page * pageSize)
					.Take(pageSize)
					.Select(g => new {
						g.Id,
						g.Name,
						g.Description,
						g.Region,
						g.Year,
						RomCount = g.Roms.Count,
						TotalSize = g.TotalSize
					})
					.ToListAsync(ct);

				return Results.Ok(new {
					Items = games,
					Total = total,
					Page = page,
					PageSize = pageSize,
					TotalPages = (int)Math.Ceiling((double)total / pageSize)
				});
			});

		// Get category tree
		group.MapGet("/categories", async (SubromDbContext db, CancellationToken ct) => {
			var paths = await db.DatFiles
				.AsNoTracking()
				.Where(d => d.CategoryPath != null)
				.Select(d => d.CategoryPath!)
				.Distinct()
				.ToListAsync(ct);

			// Build tree structure
			var tree = BuildCategoryTree(paths);
			return Results.Ok(tree);
		});

		// Delete DAT file
		group.MapDelete("/{id:guid}", async (Guid id, SubromDbContext db, CancellationToken ct) => {
			var datFile = await db.DatFiles.FindAsync([id], ct);
			if (datFile is null) {
				return Results.NotFound();
			}

			db.DatFiles.Remove(datFile);
			await db.SaveChangesAsync(ct);

			return Results.NoContent();
		});

		// Toggle enabled status
		group.MapPost("/{id:guid}/toggle", async (Guid id, SubromDbContext db, CancellationToken ct) => {
			var datFile = await db.DatFiles.FindAsync([id], ct);
			if (datFile is null) {
				return Results.NotFound();
			}

			datFile.IsEnabled = !datFile.IsEnabled;
			await db.SaveChangesAsync(ct);

			return Results.Ok(new { datFile.Id, datFile.IsEnabled });
		});

		// Apply 1G1R filter to a DAT file
		group.MapPost("/{id:guid}/1g1r", async (
			Guid id,
			OneGameOneRomRequest? request,
			SubromDbContext db,
			IOneGameOneRomService ogService,
			CancellationToken ct) => {
				// Load DAT with games and ROMs
				var datFile = await db.DatFiles
					.Include(d => d.Games)
					.ThenInclude(g => g.Roms)
					.FirstOrDefaultAsync(d => d.Id == id, ct);

				if (datFile is null) {
					return Results.NotFound(new { Message = $"DAT file {id} not found" });
				}

				// Build options from request
				var options = new OneGameOneRomOptions {
					RegionPriority = request?.RegionPriority ?? ["USA", "Europe", "Japan", "World"],
					LanguagePriority = request?.LanguagePriority ?? ["En", "De", "Fr", "Es", "It", "Ja"],
					PreferParent = request?.PreferParent ?? true,
					PreferLatestRevision = request?.PreferLatestRevision ?? true,
					PreferVerified = request?.PreferVerified ?? true,
					ExcludeCategories = request?.ExcludeCategories ?? ["Beta", "Proto", "Sample", "Demo", "Program"]
				};

				// Convert games to RomCandidate
				var candidates = datFile.Games.Select(g => new RomCandidate {
					FilePath = g.Roms.FirstOrDefault()?.Name ?? g.Name,
					Name = g.Name,
					CleanName = ExtractCleanName(g.Name),
					Region = g.Region,
					Languages = g.Languages,
					Categories = ExtractCategories(g.Name),
					Revision = ExtractRevision(g.Name),
					Parent = g.CloneOf,
					IsVerified = true, // DAT entries are always verified
					Size = g.TotalSize,
					Crc = g.Roms.FirstOrDefault()?.Crc
				}).ToList();

				// Apply 1G1R filter
				var groups = ogService.GroupAndSelect(candidates, options);

				// Build response
				var filtered = groups.Select(g => new {
					GameName = g.GameName,
					SelectedGame = new {
						g.Selected.Name,
						g.Selected.Region,
						g.Selected.Languages,
						Score = ogService.ScoreRom(g.Selected, options)
					},
					Alternatives = g.AllRoms.Where(r => r != g.Selected).Take(5).Select(a => new {
						a.Name,
						a.Region,
						Score = ogService.ScoreRom(a, options)
					}),
					AlternativeCount = g.AllRoms.Count - 1
				}).ToList();

				return Results.Ok(new {
					DatFileId = datFile.Id,
					DatFileName = datFile.Name,
					TotalGames = datFile.GameCount,
					FilteredGames = filtered.Count,
					ExcludedGames = datFile.GameCount - filtered.Count,
					Options = options,
					Games = filtered.Take(100) // Limit response
				});
			});

		// Get parent/clone analysis for a DAT file
		group.MapGet("/{id:guid}/parent-clone", async (
			Guid id,
			int limit,
			SubromDbContext db,
			IParentCloneService parentCloneService,
			CancellationToken ct) => {
				limit = Math.Clamp(limit, 1, 200);

				var datFile = await db.DatFiles
					.AsNoTracking()
					.FirstOrDefaultAsync(d => d.Id == id, ct);

				if (datFile is null) {
					return Results.NotFound(new { Message = $"DAT file {id} not found" });
				}

				// Build parent/clone index from DAT
				var index = await parentCloneService.BuildIndexFromDatAsync(id, ct);
				var groups = index.GetAllGroups();

				return Results.Ok(new {
					DatFileId = datFile.Id,
					DatFileName = datFile.Name,
					TotalGames = datFile.GameCount,
					ParentCount = index.ParentCount,
					CloneCount = index.CloneCount,
					StandaloneCount = datFile.GameCount - index.ParentCount - index.CloneCount,
					BuiltAt = index.BuiltAt,
					Groups = groups
						.OrderByDescending(g => g.Clones.Count)
						.Take(limit)
						.Select(g => new {
							Parent = g.Parent,
							CloneCount = g.Clones.Count,
							Clones = g.Clones.Take(10) // Limit clones per group
						})
				});
			});

		// Look up parent for a specific game
		group.MapGet("/{id:guid}/parent-clone/{gameName}", async (
			Guid id,
			string gameName,
			SubromDbContext db,
			IParentCloneService parentCloneService,
			CancellationToken ct) => {
				var datFile = await db.DatFiles
					.AsNoTracking()
					.FirstOrDefaultAsync(d => d.Id == id, ct);

				if (datFile is null) {
					return Results.NotFound(new { Message = $"DAT file {id} not found" });
				}

				var parent = await parentCloneService.GetParentAsync(gameName, id, ct);
				var clones = await parentCloneService.GetClonesAsync(gameName, id, ct);

				var isClone = parent is not null;
				var isParent = clones.Count > 0;

				return Results.Ok(new {
					GameName = gameName,
					IsClone = isClone,
					IsParent = isParent,
					IsStandalone = !isClone && !isParent,
					Parent = parent,
					Clones = clones.Take(50),
					TotalClones = clones.Count
				});
			});

		return endpoints;
	}

	private static List<CategoryNode> BuildCategoryTree(IEnumerable<string> paths) {
		var root = new Dictionary<string, CategoryNode>();

		foreach (var path in paths) {
			var parts = path.Split('/');
			var current = root;

			for (int i = 0; i < parts.Length; i++) {
				var part = parts[i];
				if (!current.ContainsKey(part)) {
					current[part] = new CategoryNode {
						Name = part,
						Path = string.Join("/", parts.Take(i + 1)),
						Children = []
					};
				}

				if (i < parts.Length - 1) {
					current = current[part].Children;
				}
			}
		}

		return root.Values.OrderBy(n => n.Name).ToList();
	}

	private static string ExtractCleanName(string name) {
		// Remove region, version, and other tags like "(USA) (Rev 1)"
		var clean = System.Text.RegularExpressions.Regex.Replace(name, @"\s*\([^)]+\)", "").Trim();
		return clean;
	}

	private static List<string> ExtractCategories(string name) {
		var cats = new List<string>();
		var matches = System.Text.RegularExpressions.Regex.Matches(name, @"\(([^)]+)\)");
		foreach (System.Text.RegularExpressions.Match m in matches) {
			var tag = m.Groups[1].Value;
			// Common category markers
			if (tag.StartsWith("Beta", StringComparison.OrdinalIgnoreCase) ||
				tag.StartsWith("Proto", StringComparison.OrdinalIgnoreCase) ||
				tag.StartsWith("Sample", StringComparison.OrdinalIgnoreCase) ||
				tag.StartsWith("Demo", StringComparison.OrdinalIgnoreCase) ||
				tag.Equals("Unl", StringComparison.OrdinalIgnoreCase) ||
				tag.StartsWith("Pirate", StringComparison.OrdinalIgnoreCase)) {
				cats.Add(tag);
			}
		}

		return cats;
	}

	private static int ExtractRevision(string name) {
		var match = System.Text.RegularExpressions.Regex.Match(name, @"\(Rev\s*(\d+)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		if (match.Success && int.TryParse(match.Groups[1].Value, out var rev)) {
			return rev;
		}

		return 0;
	}

	private class CategoryNode {
		public required string Name { get; init; }
		public required string Path { get; init; }
		public Dictionary<string, CategoryNode> Children { get; init; } = [];
	}
}

public record OneGameOneRomRequest(
	List<string>? RegionPriority = null,
	List<string>? LanguagePriority = null,
	bool? PreferParent = null,
	bool? PreferLatestRevision = null,
	bool? PreferVerified = null,
	List<string>? ExcludeCategories = null);
