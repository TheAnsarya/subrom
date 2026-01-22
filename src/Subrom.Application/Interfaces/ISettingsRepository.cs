using Subrom.Domain.Aggregates.Settings;

namespace Subrom.Application.Interfaces;

/// <summary>
/// Repository interface for application settings persistence.
/// </summary>
public interface ISettingsRepository {
	/// <summary>
	/// Gets the application settings.
	/// </summary>
	Task<AppSettings> GetAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves the application settings.
	/// </summary>
	Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
