// Decompiled with JetBrains decompiler
// Type: CompLib.CipDecompressor
// Assembly: CompLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 1C241C1A-9F16-4768-8714-5612B5A64360
// Assembly location: D:\Repos\Games\Attribulator1\Attribulator.CLI\bin\x86\Debug\net9.0-windows\plugins\Attribulator.Plugins.SpeedProfiles\CompLib.dll

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;


namespace CompLib;

public static class CipDecompressor
{
  public static void Decompress(Stream src, Stream dst, long srcSize, out long decompressedSize)
  {
    Debug.Assert(Marshal.SizeOf<CipHeader>() == 24);
    decompressedSize = 0L;
    BinaryReader binaryReader = new BinaryReader(src);
    long num = src.Position + srcSize;
    while (src.Position < num)
    {
      byte[] numArray1 = new byte[24];
      if (binaryReader.Read(numArray1, 0, numArray1.Length) != numArray1.Length)
        throw new CompressionException("Could not read block header from source stream");
      CipHeader cipHeader = MemoryMarshal.Read<CipHeader>((ReadOnlySpan<byte>) numArray1);
      byte[] numArray2 = new byte[cipHeader.CSize - 24];
      if (binaryReader.Read(numArray2, 0, numArray2.Length) != numArray2.Length)
        throw new CompressionException($"Could not read {numArray2.Length} compressed bytes from source stream");
      dst.Seek((long) cipHeader.UPos, SeekOrigin.Begin);
      Span<byte> buffer = BlobDecompressor.Decompress((ReadOnlySpan<byte>) numArray2);
      if (buffer.Length != cipHeader.USize)
        throw new CompressionException($"Expected decompressed data to be {cipHeader.USize} bytes, but got {buffer.Length}");
      dst.Write((ReadOnlySpan<byte>) buffer);
      decompressedSize += (long) buffer.Length;
    }
    dst.Seek(0L, SeekOrigin.Begin);
  }
}
