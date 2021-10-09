namespace Subrom.Compression.SevenZip.Exceptions {
	/// <summary>
	/// The exception that is thrown when an error in input stream occurs during decoding.
	/// </summary>
	public class DataErrorException : Exception {
		public DataErrorException() : base("Data Error") { }

		public DataErrorException(string message) : base(message) { }

		public DataErrorException(string message, Exception innerException) : base(message, innerException) { }
	}
}
