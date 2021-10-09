// LzmaBench.cs

using Subrom.Compression.SevenZip;
using Subrom.Compression.SevenZip.Interfaces;

namespace SevenZip {
	/// <summary>
	/// LZMA Benchmark
	/// </summary>
	internal abstract class LzmaBench {
		private const uint kAdditionalSize = (6 << 20);
		private const uint kCompressedAdditionalSize = (1 << 10);
		private const uint kMaxLzmaPropSize = 10;

		private class CRandomGenerator {
			private uint A1;
			private uint A2;
			public CRandomGenerator() => Init();
			public void Init() { A1 = 362436069; A2 = 521288629; }
			public uint GetRnd() => ((A1 = 36969 * (A1 & 0xffff) + (A1 >> 16)) << 16) ^
					((A2 = 18000 * (A2 & 0xffff) + (A2 >> 16)));
		};

		private class CBitRandomGenerator {
			private readonly CRandomGenerator RG = new CRandomGenerator();
			private uint Value;
			private int NumBits;
			public void Init() {
				Value = 0;
				NumBits = 0;
			}
			public uint GetRnd(int numBits) {
				uint result;
				if (NumBits > numBits) {
					result = Value & (((uint)1 << numBits) - 1);
					Value >>= numBits;
					NumBits -= numBits;
					return result;
				}
				numBits -= NumBits;
				result = (Value << numBits);
				Value = RG.GetRnd();
				result |= Value & (((uint)1 << numBits) - 1);
				Value >>= numBits;
				NumBits = 32 - numBits;
				return result;
			}
		};

		private class CBenchRandomGenerator {
			private readonly CBitRandomGenerator RG = new CBitRandomGenerator();
			private uint Pos;
			private uint Rep0;

			public uint BufferSize;
			public byte[] Buffer = null;

			public CBenchRandomGenerator() { }

			public void Set(uint bufferSize) {
				Buffer = new byte[bufferSize];
				Pos = 0;
				BufferSize = bufferSize;
			}

			private uint GetRndBit() => RG.GetRnd(1);

			private uint GetLogRandBits(int numBits) {
				var len = RG.GetRnd(numBits);
				return RG.GetRnd((int)len);
			}

			private uint GetOffset() {
				if (GetRndBit() == 0) {
					return GetLogRandBits(4);
				}

				return (GetLogRandBits(4) << 10) | RG.GetRnd(10);
			}

			private uint GetLen1() => RG.GetRnd(1 + (int)RG.GetRnd(2));

			private uint GetLen2() => RG.GetRnd(2 + (int)RG.GetRnd(2));
			public void Generate() {
				RG.Init();
				Rep0 = 1;
				while (Pos < BufferSize) {
					if (GetRndBit() == 0 || Pos < 1) {
						Buffer[Pos++] = (byte)RG.GetRnd(8);
					} else {
						uint len;
						if (RG.GetRnd(3) == 0) {
							len = 1 + GetLen1();
						} else {
							do {
								Rep0 = GetOffset();
							}
							while (Rep0 >= Pos);
							Rep0++;
							len = 2 + GetLen2();
						}
						for (uint i = 0; i < len && Pos < BufferSize; i++, Pos++) {
							Buffer[Pos] = Buffer[Pos - Rep0];
						}
					}
				}
			}
		};

		private class CrcOutStream : System.IO.Stream {
			public CRC CRC = new CRC();
			public void Init() => CRC.Init();
			public uint GetDigest() => CRC.GetDigest();

			public override bool CanRead => false;
			public override bool CanSeek => false;
			public override bool CanWrite => true;
			public override long Length => 0;
			public override long Position { get => 0; set { } }
			public override void Flush() { }
			public override long Seek(long offset, SeekOrigin origin) => 0;
			public override void SetLength(long value) { }
			public override int Read(byte[] buffer, int offset, int count) => 0;

			public override void WriteByte(byte b) => CRC.UpdateByte(b);
			public override void Write(byte[] buffer, int offset, int count) => CRC.Update(buffer, (uint)offset, (uint)count);
		};

		private class CProgressInfo : ICodeProgress {
			public long ApprovedStart;
			public long InSize;
			public System.DateTime Time;
			public void Init() => InSize = 0;
			public void SetProgress(long inSize, long outSize) {
				if (inSize >= ApprovedStart && InSize == 0) {
					Time = DateTime.UtcNow;
					InSize = inSize;
				}
			}
		}

		private const int kSubBits = 8;

		private static uint GetLogSize(uint size) {
			for (var i = kSubBits; i < 32; i++) {
				for (uint j = 0; j < (1 << kSubBits); j++) {
					if (size <= (((uint)1) << i) + (j << (i - kSubBits))) {
						return (uint)(i << kSubBits) + j;
					}
				}
			}

			return (32 << kSubBits);
		}

		private static ulong MyMultDiv64(ulong value, ulong elapsedTime) {
			ulong freq = TimeSpan.TicksPerSecond;
			var elTime = elapsedTime;
			while (freq > 1000000) {
				freq >>= 1;
				elTime >>= 1;
			}
			if (elTime == 0) {
				elTime = 1;
			}

			return value * freq / elTime;
		}

		private static ulong GetCompressRating(uint dictionarySize, ulong elapsedTime, ulong size) {
			ulong t = GetLogSize(dictionarySize) - (18 << kSubBits);
			var numCommandsForOne = 1060 + ((t * t * 10) >> (2 * kSubBits));
			var numCommands = size * numCommandsForOne;
			return MyMultDiv64(numCommands, elapsedTime);
		}

		private static ulong GetDecompressRating(ulong elapsedTime, ulong outSize, ulong inSize) {
			var numCommands = inSize * 220 + outSize * 20;
			return MyMultDiv64(numCommands, elapsedTime);
		}

		private static ulong GetTotalRating(
			uint dictionarySize,
			ulong elapsedTimeEn, ulong sizeEn,
			ulong elapsedTimeDe,
			ulong inSizeDe, ulong outSizeDe) => (GetCompressRating(dictionarySize, elapsedTimeEn, sizeEn) +
				GetDecompressRating(elapsedTimeDe, inSizeDe, outSizeDe)) / 2;

		private static void PrintValue(ulong v) {
			var s = v.ToString();
			for (var i = 0; i + s.Length < 6; i++) {
				System.Console.Write(" ");
			}

			System.Console.Write(s);
		}

		private static void PrintRating(ulong rating) {
			PrintValue(rating / 1000000);
			System.Console.Write(" MIPS");
		}

		private static void PrintResults(
			uint dictionarySize,
			ulong elapsedTime,
			ulong size,
			bool decompressMode, ulong secondSize) {
			var speed = MyMultDiv64(size, elapsedTime);
			PrintValue(speed / 1024);
			System.Console.Write(" KB/s  ");
			ulong rating;
			if (decompressMode) {
				rating = GetDecompressRating(elapsedTime, size, secondSize);
			} else {
				rating = GetCompressRating(dictionarySize, elapsedTime, size);
			}

			PrintRating(rating);
		}

		public static int LzmaBenchmark(int numIterations, uint dictionarySize) {
			if (numIterations <= 0) {
				return 0;
			}

			if (dictionarySize < (1 << 18)) {
				System.Console.WriteLine("\nError: dictionary size for benchmark must be >= 19 (512 KB)");
				return 1;
			}
			System.Console.Write("\n       Compressing                Decompressing\n\n");

			var encoder = new Compression.LZMA.Encoder();
			var decoder = new Compression.LZMA.Decoder();


			CoderPropID[] propIDs =
			{
				CoderPropID.DictionarySize,
			};
			object[] properties =
			{
				(int)(dictionarySize),
			};

			var kBufferSize = dictionarySize + kAdditionalSize;
			var kCompressedBufferSize = (kBufferSize / 2) + kCompressedAdditionalSize;

			encoder.SetCoderProperties(propIDs, properties);
			var propStream = new System.IO.MemoryStream();
			encoder.WriteCoderProperties(propStream);
			var propArray = propStream.ToArray();

			var rg = new CBenchRandomGenerator();

			rg.Set(kBufferSize);
			rg.Generate();
			var crc = new CRC();
			crc.Init();
			crc.Update(rg.Buffer, 0, rg.BufferSize);

			var progressInfo = new CProgressInfo {
				ApprovedStart = dictionarySize
			};

			ulong totalBenchSize = 0;
			ulong totalEncodeTime = 0;
			ulong totalDecodeTime = 0;
			ulong totalCompressedSize = 0;

			var inStream = new MemoryStream(rg.Buffer, 0, (int)rg.BufferSize);
			var compressedStream = new MemoryStream((int)kCompressedBufferSize);
			var crcOutStream = new CrcOutStream();
			for (var i = 0; i < numIterations; i++) {
				progressInfo.Init();
				inStream.Seek(0, SeekOrigin.Begin);
				compressedStream.Seek(0, SeekOrigin.Begin);
				encoder.Code(inStream, compressedStream, -1, -1, progressInfo);
				var sp2 = DateTime.UtcNow - progressInfo.Time;
				var encodeTime = (ulong)sp2.Ticks;

				var compressedSize = compressedStream.Position;
				if (progressInfo.InSize == 0) {
					throw (new Exception("Internal ERROR 1282"));
				}

				ulong decodeTime = 0;
				for (var j = 0; j < 2; j++) {
					compressedStream.Seek(0, SeekOrigin.Begin);
					crcOutStream.Init();

					decoder.SetDecoderProperties(propArray);
					ulong outSize = kBufferSize;
					var startTime = DateTime.UtcNow;
					decoder.Code(compressedStream, crcOutStream, 0, (long)outSize, null);
					var sp = (DateTime.UtcNow - startTime);
					decodeTime = (ulong)sp.Ticks;
					if (crcOutStream.GetDigest() != crc.GetDigest()) {
						throw (new Exception("CRC Error"));
					}
				}
				var benchSize = kBufferSize - (ulong)progressInfo.InSize;
				PrintResults(dictionarySize, encodeTime, benchSize, false, 0);
				System.Console.Write("     ");
				PrintResults(dictionarySize, decodeTime, kBufferSize, true, (ulong)compressedSize);
				System.Console.WriteLine();

				totalBenchSize += benchSize;
				totalEncodeTime += encodeTime;
				totalDecodeTime += decodeTime;
				totalCompressedSize += (ulong)compressedSize;
			}
			System.Console.WriteLine("---------------------------------------------------");
			PrintResults(dictionarySize, totalEncodeTime, totalBenchSize, false, 0);
			System.Console.Write("     ");
			PrintResults(dictionarySize, totalDecodeTime,
					kBufferSize * (ulong)numIterations, true, totalCompressedSize);
			System.Console.WriteLine("    Average");
			return 0;
		}
	}
}
