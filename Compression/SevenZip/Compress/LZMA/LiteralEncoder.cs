using Subrom.Compression.SevenZip.Compress.RangeCoder;

namespace SevenZip.Compression.LZMA {
	public partial class Encoder {
		internal class LiteralEncoder {
			public struct Encoder2 {
				private BitEncoder[] m_Encoders;

				public void Create() => m_Encoders = new BitEncoder[0x300];

				public void Init() {
					for (var i = 0; i < 0x300; i++) {
						m_Encoders[i].Init();
					}
				}

				public void Encode(RangeCoder.Encoder rangeEncoder, byte symbol) {
					uint context = 1;
					for (var i = 7; i >= 0; i--) {
						var bit = (uint)((symbol >> i) & 1);
						m_Encoders[context].Encode(rangeEncoder, bit);
						context = (context << 1) | bit;
					}
				}

				public void EncodeMatched(RangeCoder.Encoder rangeEncoder, byte matchByte, byte symbol) {
					uint context = 1;
					var same = true;
					for (var i = 7; i >= 0; i--) {
						var bit = (uint)((symbol >> i) & 1);
						var state = context;
						if (same) {
							var matchBit = (uint)((matchByte >> i) & 1);
							state += (1 + matchBit) << 8;
							same = matchBit == bit;
						}

						m_Encoders[state].Encode(rangeEncoder, bit);
						context = (context << 1) | bit;
					}
				}

				public uint GetPrice(bool matchMode, byte matchByte, byte symbol) {
					uint price = 0;
					uint context = 1;
					var i = 7;
					if (matchMode) {
						for (; i >= 0; i--) {
							var matchBit = (uint)(matchByte >> i) & 1;
							var bit = (uint)(symbol >> i) & 1;
							price += m_Encoders[((1 + matchBit) << 8) + context].GetPrice(bit);
							context = (context << 1) | bit;
							if (matchBit != bit) {
								i--;
								break;
							}
						}
					}

					for (; i >= 0; i--) {
						var bit = (uint)(symbol >> i) & 1;
						price += m_Encoders[context].GetPrice(bit);
						context = (context << 1) | bit;
					}

					return price;
				}
			}

			private Encoder2[] m_Coders;
			private int m_NumPrevBits;
			private int m_NumPosBits;
			private uint m_PosMask;

			public void Create(int numPosBits, int numPrevBits) {
				if (m_Coders != null && m_NumPrevBits == numPrevBits && m_NumPosBits == numPosBits) {
					return;
				}

				m_NumPosBits = numPosBits;
				m_PosMask = ((uint)1 << numPosBits) - 1;
				m_NumPrevBits = numPrevBits;
				var numStates = (uint)1 << (m_NumPrevBits + m_NumPosBits);
				m_Coders = new Encoder2[numStates];
				for (uint i = 0; i < numStates; i++) {
					m_Coders[i].Create();
				}
			}

			public void Init() {
				var numStates = (uint)1 << (m_NumPrevBits + m_NumPosBits);
				for (uint i = 0; i < numStates; i++) {
					m_Coders[i].Init();
				}
			}

			public Encoder2 GetSubCoder(uint pos, byte prevByte) => m_Coders[((pos & m_PosMask) << m_NumPrevBits) + (uint)(prevByte >> (8 - m_NumPrevBits))];
		}
	}
}
