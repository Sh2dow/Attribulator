// Decompiled with JetBrains decompiler
// Type: CompLib.BlobCompressor
// Assembly: CompLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 1C241C1A-9F16-4768-8714-5612B5A64360
// Assembly location: D:\Repos\Games\Attribulator1\Attribulator.CLI\bin\x86\Debug\net9.0-windows\plugins\Attribulator.Plugins.SpeedProfiles\CompLib.dll

using System;


namespace CompLib;

public static class BlobCompressor
{
  public static Span<byte> Compress(ReadOnlySpan<byte> data, IAlgorithmCompression algorithm)
  {
    byte[] array = new byte[data.Length * 2];
    int newSize = algorithm.Compress(data, (Span<byte>) array);
    if (newSize < 16 /*0x10*/)
      throw new CompressionException($"Compressed data is shorter than [sizeof(LZHeader)] ({newSize} < 16)");
    if (newSize > array.Length)
      throw new CompressionException("Length of compressed data somehow exceeds allocated size");
    Array.Resize<byte>(ref array, newSize);
    return (Span<byte>) array;
  }

  public static Span<byte> CompressBest(
    ReadOnlySpan<byte> data,
    params IAlgorithmCompression[] algorithms)
  {
    if (algorithms.Length == 0)
      throw new CompressionException("No compression algorithms specified - defaults will not be assumed!");
    Span<byte> span1 = Span<byte>.Empty;
    foreach (IAlgorithmCompression algorithm in algorithms)
    {
      Span<byte> span2 = BlobCompressor.Compress(data, algorithm);
      if (span1.Length == 0 || span2.Length < span1.Length)
        span1 = span2;
    }
    return span1;
  }
}
