using System;

namespace CompLib;

public interface IAlgorithmCompression
{
  int Compress(ReadOnlySpan<byte> input, Span<byte> output);
}
