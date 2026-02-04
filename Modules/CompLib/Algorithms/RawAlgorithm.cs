using System;

namespace CompLib.Algorithms;

public class RawAlgorithm : IAlgorithmDecompression
{
  internal const uint AlgorithmId = 1465336146;

  public int Decompress(ReadOnlySpan<byte> input, Span<byte> output)
  {
    input.CopyTo(output);
    return output.Length;
  }
}
