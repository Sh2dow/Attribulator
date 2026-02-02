// Decompiled with JetBrains decompiler
// Type: CompLib.Algorithms.JdlzAlgorithm
// Assembly: CompLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 1C241C1A-9F16-4768-8714-5612B5A64360
// Assembly location: D:\Repos\Games\Attribulator1\Attribulator.CLI\bin\x86\Debug\net9.0-windows\plugins\Attribulator.Plugins.SpeedProfiles\CompLib.dll

using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;


namespace CompLib.Algorithms;

public class JdlzAlgorithm : IAlgorithmCompression, IAlgorithmDecompression
{
  internal const uint AlgorithmId = 1514947658;
  private const int NearRunBits = 12;
  private const int NearOffBits = 4;
  private const int NearRunMax = 4098;
  private const int NearOffMax = 16 /*0x10*/;
  private const int NearRunMask = 15;
  private const int FarRunBits = 5;
  private const int FarRunMax = 34;
  private const int FarRunMask = 31 /*0x1F*/;

  public int Compress(ReadOnlySpan<byte> input, Span<byte> output)
  {
    JdlzAlgorithm.CompressorHashPool compressorHashPool1 = new JdlzAlgorithm.CompressorHashPool(2064);
    ushort num1 = 65280;
    ushort num2 = 65280;
    int index1 = 16 /*0x10*/;
    int index2 = 17;
    int num3 = 18;
    int num4 = 0;
    int length = input.Length;
    Span<byte> span1 = (Span<byte>) new byte[length + 3];
    input.CopyTo(span1.Slice(0, length));
    ref Span<byte> local1 = ref span1;
    ref byte local2 = ref local1[local1.Length - 3];
    ref Span<byte> local3 = ref span1;
    ref byte local4 = ref local3[local3.Length - 2];
    ref Span<byte> local5 = ref span1;
    int num5;
    byte num6 = (byte) (num5 = (int) (local5[local5.Length - 1] = (byte) 253));
    local4 = (byte) num5;
    int num7 = (int) num6;
    local2 = (byte) num7;
    while (length >= 0)
    {
      int num8 = Math.Min(length, 4098);
      JdlzAlgorithm.CompressorHashPool compressorHashPool2 = compressorHashPool1;
      Span<byte> span2 = span1;
      int start1 = num4;
      ReadOnlySpan<byte> data1 = (ReadOnlySpan<byte>) span2.Slice(start1, span2.Length - start1);
      JdlzAlgorithm.CompressorHash compressorHash1 = compressorHashPool2.FindHashBucket(data1);
      int val1 = 2;
      JdlzAlgorithm.CompressorHash compressorHash2 = (JdlzAlgorithm.CompressorHash) null;
      for (; compressorHash1 != null && val1 < 4098; compressorHash1 = compressorHash1.Next)
      {
        span2 = span1;
        int offset = compressorHash1.Offset;
        ReadOnlySpan<byte> in1 = (ReadOnlySpan<byte>) span2.Slice(offset, span2.Length - offset);
        span2 = span1;
        int start2 = num4;
        ReadOnlySpan<byte> in2 = (ReadOnlySpan<byte>) span2.Slice(start2, span2.Length - start2);
        int n = num8;
        int num9 = JdlzAlgorithm.Compare(in1, in2, n);
        if (num9 > val1)
        {
          int num10 = num4 - compressorHash1.Offset;
          if (num9 > 34 && (num10 < 16 /*0x10*/ || val1 <= 34) || num9 <= 34)
          {
            val1 = num9;
            compressorHash2 = compressorHash1;
          }
        }
      }
      if (val1 > 2)
      {
        Contract.Assume(compressorHash2 != null);
        num1 >>= 1;
        int num11 = num4 - compressorHash2.Offset - 1;
        if (num11 >= 16 /*0x10*/)
        {
          val1 = Math.Min(val1, 34);
          num2 = (ushort) ((int) num2 >> 1 & 65407);
          ref Span<byte> local6 = ref output;
          int index3 = num3;
          int num12 = index3 + 1;
          local6[index3] = (byte) (num11 - 16 /*0x10*/ >> 8 << 5 | val1 - 3);
          ref Span<byte> local7 = ref output;
          int index4 = num12;
          num3 = index4 + 1;
          local7[index4] = (byte) (num11 - 16 /*0x10*/ & (int) byte.MaxValue);
        }
        else
        {
          num2 >>= 1;
          ref Span<byte> local8 = ref output;
          int index5 = num3;
          int num13 = index5 + 1;
          local8[index5] = (byte) (val1 - 3 >> 8 << 4 | num11 & (int) byte.MaxValue);
          ref Span<byte> local9 = ref output;
          int index6 = num13;
          num3 = index6 + 1;
          local9[index6] = (byte) (val1 - 3 & (int) byte.MaxValue);
        }
        length -= val1;
        do
        {
          JdlzAlgorithm.CompressorHashPool compressorHashPool3 = compressorHashPool1;
          span2 = span1;
          int start3 = num4;
          ReadOnlySpan<byte> data2 = (ReadOnlySpan<byte>) span2.Slice(start3, span2.Length - start3);
          int offset = num4;
          compressorHashPool3.Update(data2, offset);
          ++num4;
        }
        while (--val1 > 0);
      }
      else
      {
        JdlzAlgorithm.CompressorHashPool compressorHashPool4 = compressorHashPool1;
        span2 = span1;
        int start4 = num4;
        ReadOnlySpan<byte> data3 = (ReadOnlySpan<byte>) span2.Slice(start4, span2.Length - start4);
        int offset = num4;
        compressorHashPool4.Update(data3, offset);
        output[num3++] = span1[num4++];
        num1 = (ushort) ((int) num1 >> 1 & 65407);
        --length;
      }
      if (num1 < (ushort) 256 /*0x0100*/)
      {
        output[index1] = (byte) ((uint) num1 & (uint) byte.MaxValue);
        num1 = (ushort) 65280;
        index1 = num3++;
      }
      if (num2 < (ushort) 256 /*0x0100*/)
      {
        output[index2] = (byte) ((uint) num2 & (uint) byte.MaxValue);
        num2 = (ushort) 65280;
        index2 = num3++;
      }
    }
    while (((uint) num1 & 65280U) > 0U)
      num1 >>= 1;
    while (((uint) num2 & 65280U) > 0U)
      num2 >>= 1;
    output[index1] = (byte) ((uint) num1 & (uint) byte.MaxValue);
    output[index2] = (byte) ((uint) num2 & (uint) byte.MaxValue);
    CompressedDataHeader compressedDataHeader = new CompressedDataHeader()
    {
      ID = 1514947658,
      Version = 2,
      Flags = 0,
      HeaderSize = 16 /*0x10*/,
      UncompressedSize = (uint) input.Length,
      CompressedSize = (uint) num3
    };
    MemoryMarshal.Write<CompressedDataHeader>(output, ref compressedDataHeader);
    return num3;
  }

  public int Decompress(ReadOnlySpan<byte> input, Span<byte> output)
  {
    int num1 = 0;
    int num2 = 0;
    ref ReadOnlySpan<byte> local1 = ref input;
    int index1 = num1;
    int num3 = index1 + 1;
    uint num4 = (uint) (256 /*0x0100*/ | (int) local1[index1]);
    ref ReadOnlySpan<byte> local2 = ref input;
    int index2 = num3;
    int num5 = index2 + 1;
    uint num6 = (uint) (256 /*0x0100*/ | (int) local2[index2]);
    while (num5 < input.Length && num2 < output.Length)
    {
      if (((int) num4 & 1) == 0)
      {
        do
        {
          output[num2++] = input[num5++];
          num4 >>= 1;
        }
        while (((int) num4 & 1) == 0 && num2 < output.Length);
        if (num4 == 1U)
          num4 = 256U /*0x0100*/ | (uint) input[num5++];
      }
      else
      {
        ref ReadOnlySpan<byte> local3 = ref input;
        int index3 = num5;
        int num7 = index3 + 1;
        uint num8 = (uint) local3[index3];
        int num9;
        int num10;
        if ((num6 & 1U) > 0U)
        {
          num9 = ((int) num8 & 15) + 1;
          int num11 = (int) (num8 >> 4) << 8;
          ref ReadOnlySpan<byte> local4 = ref input;
          int index4 = num7;
          num5 = index4 + 1;
          int num12 = (int) local4[index4];
          num10 = (num11 | num12) + 3;
        }
        else
        {
          int num13 = (int) (num8 >> 5) << 8;
          ref ReadOnlySpan<byte> local5 = ref input;
          int index5 = num7;
          num5 = index5 + 1;
          int num14 = (int) local5[index5];
          num9 = (num13 | num14) + 16 /*0x10*/ + 1;
          num10 = ((int) num8 & 31 /*0x1F*/) + 3;
        }
        for (int index6 = 0; index6 < num10; ++index6)
          output[num2 + index6] = output[num2 - num9 + index6];
        num2 += num10;
        num4 >>= 1;
        if (num4 == 1U)
          num4 = 256U /*0x0100*/ | (uint) input[num5++];
        num6 >>= 1;
        if (num6 == 1U)
          num6 = 256U /*0x0100*/ | (uint) input[num5++];
      }
    }
    return num2;
  }

  private static int Compare(ReadOnlySpan<byte> in1, ReadOnlySpan<byte> in2, int n)
  {
    int index = 0;
    while (index < n && (int) in1[index] == (int) in2[index])
      ++index;
    return index;
  }

  private static int Hash(ReadOnlySpan<byte> data)
  {
    int num = 0;
    if (data.Length > 2)
      num = (int) data[2] << 8;
    if (data.Length > 1)
      num ^= (int) data[1] << 4;
    if (data.Length > 0)
      num ^= (int) data[0];
    return num * 40543 & 8191 /*0x1FFF*/;
  }

  private class CompressorHash
  {
    public CompressorHash(
      int offset,
      JdlzAlgorithm.CompressorHash? previous,
      JdlzAlgorithm.CompressorHash? next)
    {
      this.CurrentBucket = -1;
      this.Offset = offset;
      this.Previous = previous;
      this.Next = next;
    }

    public int Offset { get; set; }

    public int CurrentBucket { get; set; }

    public JdlzAlgorithm.CompressorHash? Previous { get; set; }

    public JdlzAlgorithm.CompressorHash? Next { get; set; }
  }

  private class CompressorHashPool
  {
    private readonly JdlzAlgorithm.CompressorHash?[] _bucketHeads;
    private readonly JdlzAlgorithm.CompressorHash[] _pool;

    internal CompressorHashPool(int poolSize)
    {
      this._pool = new JdlzAlgorithm.CompressorHash[poolSize];
      this._bucketHeads = new JdlzAlgorithm.CompressorHash[8192 /*0x2000*/];
      for (int index = 0; index < poolSize; ++index)
        this._pool[index] = new JdlzAlgorithm.CompressorHash(0, (JdlzAlgorithm.CompressorHash) null, (JdlzAlgorithm.CompressorHash) null);
    }

    internal void Update(ReadOnlySpan<byte> data, int offset)
    {
      JdlzAlgorithm.CompressorHash compressorHash = this._pool[offset % this._pool.Length];
      if (compressorHash.CurrentBucket != -1)
      {
        if (compressorHash.Next != null)
          compressorHash.Next.Previous = compressorHash.Previous;
        if (compressorHash.Previous != null)
          compressorHash.Previous.Next = compressorHash.Next;
        if (this._bucketHeads[compressorHash.CurrentBucket] == compressorHash)
          this._bucketHeads[compressorHash.CurrentBucket] = compressorHash.Next;
      }
      compressorHash.Offset = offset;
      int index = JdlzAlgorithm.Hash(data);
      compressorHash.Previous = (JdlzAlgorithm.CompressorHash) null;
      compressorHash.Next = this._bucketHeads[index];
      this._bucketHeads[index] = compressorHash;
      compressorHash.CurrentBucket = index;
      if (compressorHash.Next == null)
        return;
      compressorHash.Next.Previous = compressorHash;
    }

    internal JdlzAlgorithm.CompressorHash? FindHashBucket(ReadOnlySpan<byte> data)
    {
      return this._bucketHeads[JdlzAlgorithm.Hash(data)];
    }
  }
}
