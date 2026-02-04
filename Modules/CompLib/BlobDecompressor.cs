using CompLib.Algorithms;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CompLib;

public static class BlobDecompressor
{
  public static Span<byte> Decompress(ReadOnlySpan<byte> input)
  {
    CompressedDataHeader header = input.Length >= 16 /*0x10*/ ? MemoryMarshal.Read<CompressedDataHeader>(input) : throw new CompressionException($"Expected at least 16 bytes to be passed to decompressor, but only got {input.Length}.");
    IAlgorithmDecompression algorithmFromHeader = BlobDecompressor.GetAlgorithmFromHeader(header);
    Span<byte> span = (Span<byte>) new byte[(int) header.UncompressedSize];
    IAlgorithmDecompression algorithmDecompression = algorithmFromHeader;
    ReadOnlySpan<byte> readOnlySpan = input;
    int headerSize = (int) header.HeaderSize;
    ReadOnlySpan<byte> input1 = readOnlySpan.Slice(headerSize, readOnlySpan.Length - headerSize);
    Span<byte> output = span;
    int num = algorithmDecompression.Decompress(input1, output);
    if (num != span.Length)
      throw new CompressionException($"Expected to end up with {span.Length} decompressed bytes, but only got {num} bytes");
    return span;
  }

  public static Span<byte> Decompress(BinaryReader reader)
  {
    byte[] source = reader.ReadBytes(16 /*0x10*/);
    CompressedDataHeader header = source.Length >= 16 /*0x10*/ ? MemoryMarshal.Read<CompressedDataHeader>((ReadOnlySpan<byte>) source) : throw new CompressionException($"Expected at least 16 bytes to be available for header, but only got {source.Length}.");
    IAlgorithmDecompression algorithmFromHeader = BlobDecompressor.GetAlgorithmFromHeader(header);
    uint id = header.ID;
    if (true)
      ;
    uint length = id == 1514947658U ? header.CompressedSize - 16U /*0x10*/ : header.CompressedSize;
    if (true)
      ;
    Span<byte> span = (Span<byte>) new byte[(int) length];
    Span<byte> output = (Span<byte>) new byte[(int) header.UncompressedSize];
    int num1 = reader.Read(span);
    if (num1 != span.Length)
      throw new CompressionException($"Expected to read {span.Length} compressed bytes, but only got {num1} bytes");
    int num2 = algorithmFromHeader.Decompress((ReadOnlySpan<byte>) span, output);
    if (num2 != output.Length)
      throw new CompressionException($"Expected to end up with {output.Length} decompressed bytes, but only got {num2} bytes");
    return output;
  }

  private static IAlgorithmDecompression GetAlgorithmFromHeader(CompressedDataHeader header)
  {
    uint id = header.ID;
    if (true)
      ;
    IAlgorithmDecompression algorithmFromHeader;
    switch (id)
    {
      case 1179014472:
        algorithmFromHeader = (IAlgorithmDecompression) new HuffAlgorithm();
        break;
      case 1263552082:
        algorithmFromHeader = (IAlgorithmDecompression) new RefPackAlgorithm();
        break;
      case 1465336146:
        algorithmFromHeader = (IAlgorithmDecompression) new RawAlgorithm();
        break;
      case 1514947658:
        algorithmFromHeader = (IAlgorithmDecompression) new JdlzAlgorithm();
        break;
      default:
        throw new CompressionException($"Unrecognized compression algorithm: 0x{header.ID:X8}");
    }
    if (true)
      ;
    return algorithmFromHeader;
  }
}
