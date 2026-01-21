using Subrom.Application.Interfaces;
using Subrom.Domain.Aggregates.DatFiles;

namespace Subrom.Infrastructure.Parsing;

/// <summary>
/// Factory for selecting the appropriate DAT parser based on file format.
/// </summary>
public sealed class DatParserFactory : IDatParserFactory {
	private readonly IEnumerable<IDatParser> _parsers;

	public DatParserFactory(IEnumerable<IDatParser> parsers) {
		_parsers = parsers;
	}

	public IDatParser? GetParser(string filePath) {
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		foreach (var parser in _parsers) {
			if (parser.CanParse(filePath)) {
				return parser;
			}
		}

		return null; // No parser found
	}

	public IDatParser? GetParser(DatFormat format) {
		return _parsers.FirstOrDefault(p => p.Format == format);
	}

	public IDatParser? GetParser(Stream stream) {
		ArgumentNullException.ThrowIfNull(stream);

		// For streams, we'll default to Logiqx XML parser
		// TODO: Implement stream peeking to detect format
		return GetParser(DatFormat.LogiqxXml);
	}

	public IReadOnlyList<DatFormat> GetSupportedFormats() =>
		_parsers.Select(p => p.Format).Distinct().ToList();
}
