using Microsoft.EntityFrameworkCore;
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

	private class CategoryNode {
		public required string Name { get; init; }
		public required string Path { get; init; }
		public Dictionary<string, CategoryNode> Children { get; init; } = [];
	}
}
