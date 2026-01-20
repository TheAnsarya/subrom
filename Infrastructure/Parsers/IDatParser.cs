using Subrom.Domain.Datfiles;

namespace Subrom.Infrastructure.Parsers;

/// <summary>
/// Interface for DAT file parsers.
/// </summary>
public interface IDatParser {
	/// <summary>
	/// Gets the name of the format this parser handles.
	/// </summary>
	string FormatName { get; }

	/// <summary>
	/// Determines if this parser can handle the given stream.
	/// </summary>
	bool CanParse(Stream stream);

	/// <summary>
	/// Parses the DAT file from the stream.
	/// </summary>
	Task<Datafile> ParseAsync(Stream stream, CancellationToken cancellationToken = default);
}
