﻿namespace SevenZip.Compression.RangeCoder {
	internal class Decoder {
		public const uint kTopValue = 1 << 24;
		public uint Range;
		public uint Code;
		public Stream Stream;

		public void Init(System.IO.Stream stream) {
			Stream = stream;

			Code = 0;
			Range = 0xFFFFFFFF;
			for (var i = 0; i < 5; i++) {
				Code = (Code << 8) | (byte)Stream.ReadByte();
			}
		}

		public void ReleaseStream() => Stream = null;

		public void CloseStream() => Stream.Close();

		public void Normalize() {
			while (Range < kTopValue) {
				Code = (Code << 8) | (byte)Stream.ReadByte();
				Range <<= 8;
			}
		}

		public void Normalize2() {
			if (Range < kTopValue) {
				Code = (Code << 8) | (byte)Stream.ReadByte();
				Range <<= 8;
			}
		}

		public uint GetThreshold(uint total) => Code / (Range /= total);

		public void Decode(uint start, uint size, uint total) {
			Code -= start * Range;
			Range *= size;
			Normalize();
		}

		public uint DecodeDirectBits(int numTotalBits) {
			var range = Range;
			var code = Code;
			uint result = 0;
			for (var i = numTotalBits; i > 0; i--) {
				range >>= 1;

				var t = (code - range) >> 31;
				code -= range & (t - 1);
				result = (result << 1) | (1 - t);

				if (range < kTopValue) {
					code = (code << 8) | (byte)Stream.ReadByte();
					range <<= 8;
				}
			}

			Range = range;
			Code = code;
			return result;
		}

		public uint DecodeBit(uint size0, int numTotalBits) {
			var newBound = (Range >> numTotalBits) * size0;
			uint symbol;
			if (Code < newBound) {
				symbol = 0;
				Range = newBound;
			} else {
				symbol = 1;
				Code -= newBound;
				Range -= newBound;
			}

			Normalize();
			return symbol;
		}
	}
}
