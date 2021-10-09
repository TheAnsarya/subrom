namespace SevenZip.Compression.LZMA {
	public partial class Encoder {
		internal class LenPriceTableEncoder : LenEncoder {
			private readonly uint[] _prices = new uint[Base.kNumLenSymbols << Base.kNumPosStatesBitsEncodingMax];
			private uint _tableSize;
			private readonly uint[] _counters = new uint[Base.kNumPosStatesEncodingMax];

			public void SetTableSize(uint tableSize) => _tableSize = tableSize;

			public uint GetPrice(uint symbol, uint posState) => _prices[(posState * Base.kNumLenSymbols) + symbol];

			private void UpdateTable(uint posState) {
				SetPrices(posState, _tableSize, _prices, posState * Base.kNumLenSymbols);
				_counters[posState] = _tableSize;
			}

			public void UpdateTables(uint numPosStates) {
				for (uint posState = 0; posState < numPosStates; posState++) {
					UpdateTable(posState);
				}
			}

			public new void Encode(RangeCoder.Encoder rangeEncoder, uint symbol, uint posState) {
				base.Encode(rangeEncoder, symbol, posState);
				if (--_counters[posState] == 0) {
					UpdateTable(posState);
				}
			}
		}
	}
}
