using System;

namespace CompLib;

public interface IAlgorithmDecompression
{
  int Decompress(ReadOnlySpan<byte> input, Span<byte> output);
}
