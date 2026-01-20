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

	public IDatParser GetParser(string filePath) {
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		foreach (var parser in _parsers) {
			if (parser.CanParse(filePath)) {
				return parser;
			}
		}

		throw new NotSupportedException($"No parser available for file: {Path.GetFileName(filePath)}");
	}

	public IDatParser GetParser(DatFormat format) {
		var parser = _parsers.FirstOrDefault(p => p.Format == format);
		if (parser is null) {
			throw new NotSupportedException($"No parser available for format: {format}");
		}

		return parser;
	}

	public IReadOnlyList<DatFormat> GetSupportedFormats() =>
		_parsers.Select(p => p.Format).Distinct().ToList();
}
