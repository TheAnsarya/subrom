namespace SevenZip {
	/// <summary>
	/// The exception that is thrown when the value of an argument is outside the allowable range.
	/// </summary>
	public class InvalidParamException : Exception {
		public InvalidParamException() : base("Invalid Parameter") { }

		public InvalidParamException(string message) : base(message) { }

		public InvalidParamException(string message, Exception innerException) : base(message, innerException) { }
	}
}
