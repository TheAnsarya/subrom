using SevenZip.Compression.RangeCoder;

namespace Subrom.Compression.SevenZip.Compress.RangeCoder {
	internal struct BitEncoder {
		public const int NumBitModelTotalBits = 11;
		public const uint BitModelTotal = 1 << NumBitModelTotalBits;
		private const int NumMoveBits = 5;
		private const int NumMoveReducingBits = 2;
		public const int NumBitPriceShiftBits = 6;
		private uint Prob;

		public void Init() => Prob = BitModelTotal >> 1;

		public void UpdateModel(uint symbol) {
			if (symbol == 0) {
				// TODO: where do parens go?
				Prob += BitModelTotal - Prob >> NumMoveBits;
			} else {
				Prob -= Prob >> NumMoveBits;
			}
		}

		public void Encode(Encoder encoder, uint symbol) {
			var newBound = (encoder.Range >> NumBitModelTotalBits) * Prob;
			if (symbol == 0) {
				encoder.Range = newBound;
				Prob += BitModelTotal - Prob >> NumMoveBits;
			} else {
				encoder.Low += newBound;
				encoder.Range -= newBound;
				Prob -= Prob >> NumMoveBits;
			}

			if (encoder.Range < Encoder.kTopValue) {
				encoder.Range <<= 8;
				encoder.ShiftLow();
			}
		}

		private static readonly uint[] ProbPrices = new uint[BitModelTotal >> NumMoveReducingBits];

		static BitEncoder() {
			const int kNumBits = NumBitModelTotalBits - NumMoveReducingBits;
			for (var i = kNumBits - 1; i >= 0; i--) {
				var start = (uint)1 << kNumBits - i - 1;
				var end = (uint)1 << kNumBits - i;
				for (var j = start; j < end; j++) {
					ProbPrices[j] = ((uint)i << NumBitPriceShiftBits) +
						(end - j << NumBitPriceShiftBits >> kNumBits - i - 1);
				}
			}
		}

		public uint GetPrice(uint symbol) => ProbPrices[((Prob - symbol ^ -(int)symbol) & BitModelTotal - 1) >> NumMoveReducingBits];
		public uint GetPrice0() => ProbPrices[Prob >> NumMoveReducingBits];
		public uint GetPrice1() => ProbPrices[BitModelTotal - Prob >> NumMoveReducingBits];
	}

	internal struct BitDecoder {
		public const int kNumBitModelTotalBits = 11;
		public const uint kBitModelTotal = 1 << kNumBitModelTotalBits;
		private const int kNumMoveBits = 5;
		private uint Prob;

		public void UpdateModel(int numMoveBits, uint symbol) {
			if (symbol == 0) {
				Prob += kBitModelTotal - Prob >> numMoveBits;
			} else {
				Prob -= Prob >> numMoveBits;
			}
		}

		public void Init() => Prob = kBitModelTotal >> 1;

		public uint Decode(RangeCoder.Decoder rangeDecoder) {
			var newBound = (rangeDecoder.Range >> kNumBitModelTotalBits) * Prob;
			if (rangeDecoder.Code < newBound) {
				rangeDecoder.Range = newBound;
				Prob += kBitModelTotal - Prob >> kNumMoveBits;
				if (rangeDecoder.Range < Decoder.kTopValue) {
					rangeDecoder.Code = rangeDecoder.Code << 8 | (byte)rangeDecoder.Stream.ReadByte();
					rangeDecoder.Range <<= 8;
				}

				return 0;
			} else {
				rangeDecoder.Range -= newBound;
				rangeDecoder.Code -= newBound;
				Prob -= Prob >> kNumMoveBits;
				if (rangeDecoder.Range < Decoder.kTopValue) {
					rangeDecoder.Code = rangeDecoder.Code << 8 | (byte)rangeDecoder.Stream.ReadByte();
					rangeDecoder.Range <<= 8;
				}

				return 1;
			}
		}
	}
}
