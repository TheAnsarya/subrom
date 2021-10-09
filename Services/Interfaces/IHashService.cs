using Subrom.Domain.Hash;

namespace Subrom.Services.Interfaces {
	public interface IHashService {
		Task<Hashes> GetAll(Stream stream);
	}
}
