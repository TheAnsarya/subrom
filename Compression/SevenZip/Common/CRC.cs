// Common/CRC.cs

namespace SevenZip {
	internal class CRC {
		public static readonly uint[] Table = new uint[256];
		private const uint KPoly = 0xEDB88320;

		static CRC() {
			for (uint i = 0; i < 256; i++) {
				var r = i;
				for (var j = 0; j < 8; j++) {
					if ((r & 1) != 0) {
						r = (r >> 1) ^ KPoly;
					} else {
						r >>= 1;
					}
				}

				Table[i] = r;
			}
		}

		private uint _value = 0xFFFFFFFF;

		public void UpdateByte(byte b) => _value = Table[((byte)_value) ^ b] ^ (_value >> 8);

		public void Update(byte[] data, uint offset, uint size) {
			for (uint i = 0; i < size; i++) {
				_value = Table[((byte)_value) ^ data[offset + i]] ^ (_value >> 8);
			}
		}

		public uint GetDigest() => _value ^ 0xFFFFFFFF;

		public static uint CalculateDigest(byte[] data, uint offset, uint size) {
			var crc = new CRC();
			crc.Update(data, offset, size);
			return crc.GetDigest();
		}

		public static bool VerifyDigest(uint digest, byte[] data, uint offset, uint size) => CalculateDigest(data, offset, size) == digest;
	}
}
