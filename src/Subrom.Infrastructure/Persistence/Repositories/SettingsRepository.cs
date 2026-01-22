using Microsoft.EntityFrameworkCore;
using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.Settings;

namespace Subrom.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of ISettingsRepository.
/// Uses singleton pattern - always returns/updates the single settings row.
/// </summary>
public sealed class SettingsRepository : ISettingsRepository {
	private readonly SubromDbContext _context;

	public SettingsRepository(SubromDbContext context) {
		_context = context;
	}

	public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default) {
		var settings = await _context.AppSettings
			.FirstOrDefaultAsync(s => s.Id == AppSettings.SingletonId, cancellationToken);

		if (settings is null) {
			// Create default settings if none exist
			settings = new AppSettings();
			await _context.AppSettings.AddAsync(settings, cancellationToken);
			await _context.SaveChangesAsync(cancellationToken);
		}

		return settings;
	}

	public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default) {
		// Ensure we're updating the singleton row
		settings.Id = AppSettings.SingletonId;

		var existing = await _context.AppSettings
			.FirstOrDefaultAsync(s => s.Id == AppSettings.SingletonId, cancellationToken);

		if (existing is null) {
			await _context.AppSettings.AddAsync(settings, cancellationToken);
		} else {
			// Copy values to tracked entity
			_context.Entry(existing).CurrentValues.SetValues(settings);
		}
	}
}
