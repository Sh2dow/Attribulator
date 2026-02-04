using System;

namespace CompLib.Algorithms;

public class HuffAlgorithm : IAlgorithmDecompression
{
  internal const uint AlgorithmId = 1179014472;

  public int Decompress(ReadOnlySpan<byte> input, Span<byte> output)
  {
    HuffAlgorithm.BitStream bitStream = new HuffAlgorithm.BitStream(input);
    int num1 = (int) bitStream.ReadBits(0);
    uint num2 = bitStream.ReadBits(16 /*0x10*/);
    if (((int) num2 & 256 /*0x0100*/) == 256 /*0x0100*/)
      throw new CompressionException($"Unsupported HUFF flags: 0x{num2:X}");
    uint num3 = num2 & 32768U /*0x8000*/;
    if (true)
      ;
    uint num4 = num3 == 32768U /*0x8000*/ ? bitStream.ReadBits(16 /*0x10*/) << 16 /*0x10*/ | bitStream.ReadBits(16 /*0x10*/) : bitStream.ReadBits(8) << 16 /*0x10*/ | bitStream.ReadBits(16 /*0x10*/);
    if (true)
      ;
    uint num5 = num4;
    if ((long) num5 != (long) output.Length)
      throw new CompressionException($"Expected output buffer to be {num5} bytes long, but it's {output.Length} bytes long");
    int num6 = 0;
    uint num7 = 0;
    int index1 = 1;
    byte num8 = (byte) bitStream.ReadBits(8);
    uint num9 = 0;
    Span<int> span1 = stackalloc int[16 /*0x10*/];
    Span<uint> span2 = stackalloc uint[16 /*0x10*/];
    Span<uint> span3 = stackalloc uint[16 /*0x10*/];
    Span<byte> span4 = stackalloc byte[256 /*0x0100*/];
    Span<byte> span5 = stackalloc byte[256 /*0x0100*/];
    Span<byte> span6 = stackalloc byte[256 /*0x0100*/];
    Span<sbyte> span7 = stackalloc sbyte[256 /*0x0100*/];
    span6.Fill((byte) 64 /*0x40*/);
    uint num10;
    uint num11;
    do
    {
      uint num12 = num9 << 1;
      span2[index1] = num12 - num7;
      num10 = bitStream.ReadNum();
      span1[index1] = (int) num10;
      num7 += num10;
      num9 = num12 + num10;
      if (true)
        ;
      uint num13 = num10 == 0U ? 0U : (uint) ((int) num9 << 16 /*0x10*/ - index1 & (int) ushort.MaxValue);
      if (true)
        ;
      num11 = num13;
      span3[index1++] = num11;
    }
    while (num10 == 0U || num11 != 0U);
    span3[index1 - 1] = uint.MaxValue;
    int num14 = index1 - 1;
    byte maxValue = byte.MaxValue;
    for (int index2 = 0; (long) index2 < (long) num7; ++index2)
    {
      int num15 = (int) bitStream.ReadNum() + 1;
      do
      {
        ++maxValue;
        if (span7[(int) maxValue] == (sbyte) 0)
          --num15;
      }
      while (num15 != 0);
      span7[(int) maxValue] = (sbyte) 1;
      span4[index2] = maxValue;
    }
    byte index3 = 1;
    int num16 = 0;
    int start1 = 0;
    int num17 = 0;
    Span<byte> span8;
    for (; (int) index3 <= num14 && index3 < (byte) 9; ++index3)
    {
      int num18 = span1[(int) index3];
      int num19 = 1 << 8 - (int) index3;
      while (num18-- > 0)
      {
        byte num20 = span4[num16++];
        byte num21 = index3;
        if ((int) num20 == (int) num8)
        {
          num17 = (int) index3;
          num21 = (byte) 96 /*0x60*/;
        }
        span5.Slice(start1, num19).Fill(num20);
        span6.Slice(start1, num19).Fill(num21);
        start1 += num19;
      }
    }
    while (true)
    {
      int num22 = (int) span6[(int) (bitStream.Bits >> 24)];
      for (bitStream.BitsLeft -= num22; bitStream.BitsLeft >= 0; bitStream.BitsLeft -= num22)
      {
        output[num6++] = span5[(int) (bitStream.Bits >> 24)];
        bitStream.Bits <<= num22;
        num22 = (int) span6[(int) (bitStream.Bits >> 24)];
      }
      bitStream.BitsLeft += 16 /*0x10*/;
      if (bitStream.BitsLeft >= 0)
      {
        output[num6++] = span5[(int) (bitStream.Bits >> 24)];
        bitStream.RefreshBits();
      }
      else
      {
        bitStream.BitsLeft = bitStream.BitsLeft - 16 /*0x10*/ + num22;
        int index4;
        if (num22 != 96 /*0x60*/)
        {
          uint num23 = bitStream.Bits >> 16 /*0x10*/;
          index4 = 8;
          do
          {
            ++index4;
          }
          while (num23 >= span3[index4]);
        }
        else
          index4 = num17;
        uint num24 = bitStream.Bits >> 32 /*0x20*/ - index4;
        bitStream.Bits <<= index4;
        bitStream.BitsLeft -= index4;
        byte num25 = span4[(int) num24 - (int) span2[index4]];
        if ((int) num25 != (int) num8 && bitStream.BitsLeft >= 0)
        {
          output[num6++] = num25;
        }
        else
        {
          if (bitStream.BitsLeft < 0)
          {
            bitStream.ReadSegment();
            bitStream.Bits = bitStream.UnshiftedBits << -bitStream.BitsLeft;
            bitStream.BitsLeft += 16 /*0x10*/;
          }
          if ((int) num25 != (int) num8)
          {
            output[num6++] = num25;
          }
          else
          {
            int num26 = (int) bitStream.ReadNum();
            if (num26 > 0)
            {
              byte num27 = output[num6 - 1];
              span8 = output;
              int start2 = num6;
              span8.Slice(start2, num6 + num26 - start2).Fill(num27);
              num6 += num26;
            }
            else if (bitStream.ReadBits(1) <= 0U)
              output[num6++] = (byte) bitStream.ReadBits(8);
            else
              break;
          }
        }
      }
    }
    switch ((long) num2 & -32769L)
    {
      case 13051:
        uint num28 = 0;
        for (int index5 = 0; (long) index5 < (long) num5; ++index5)
        {
          num28 += (uint) output[index5];
          output[index5] = (byte) num28;
        }
        break;
      case 13563:
        uint num29 = 0;
        uint num30 = 0;
        for (int index6 = 0; (long) index6 < (long) num5; ++index6)
        {
          num29 += (uint) output[index6];
          num30 += num29;
          output[index6] = (byte) num30;
        }
        break;
    }
    return num6;
  }

  private ref struct BitStream(ReadOnlySpan<byte> buffer)
  {
    private readonly ReadOnlySpan<byte> _buffer = buffer;
    private int _offset = 0;
    public int BitsLeft = -16;
    public uint UnshiftedBits = 0;
    public uint Bits = 0;

    private byte? ReadNextByte()
    {
      return this._offset < this._buffer.Length ? new byte?(this._buffer[this._offset++]) : new byte?();
    }

    private void ReadSegmentPart()
    {
      this.UnshiftedBits = (uint) this.ReadNextByte().GetValueOrDefault() | this.UnshiftedBits << 8;
    }

    public void ReadSegment()
    {
      this.ReadSegmentPart();
      this.ReadSegmentPart();
    }

    public uint ReadBits(int n)
    {
      if (n < 0 || n > 32 /*0x20*/)
        throw new ArgumentException($"Tried to read {n} bits, outside the valid range [0, 32]");
      uint num = 0;
      if (n > 0)
      {
        num = this.Bits >> 32 /*0x20*/ - n;
        this.Bits <<= n;
        this.BitsLeft -= n;
      }
      if (this.BitsLeft >= 0)
        return num;
      this.ReadSegment();
      this.Bits = this.UnshiftedBits << -this.BitsLeft;
      this.BitsLeft += 16 /*0x10*/;
      return num;
    }

    public uint ReadNum()
    {
      if ((int) this.Bits < 0)
        return this.ReadBits(3) - 4U;
      int n;
      if (this.Bits >> 16 /*0x10*/ > 0U)
      {
        n = 2;
        do
        {
          this.Bits <<= 1;
          ++n;
        }
        while ((int) this.Bits >= 0);
        this.Bits <<= 1;
        this.BitsLeft -= n - 1;
        int num = (int) this.ReadBits(0);
      }
      else
      {
        n = 2;
        do
        {
          ++n;
        }
        while (this.ReadBits(1) == 0U);
      }
      return n <= 16 /*0x10*/ ? (uint) ((ulong) this.ReadBits(n) + (ulong) (1 << n) - 4UL) : (uint) ((ulong) (this.ReadBits(16 /*0x10*/) | this.ReadBits(n - 16 /*0x10*/) << 16 /*0x10*/) + (ulong) (1 << n) - 4UL);
    }

    public void RefreshBits()
    {
      this.ReadSegment();
      this.Bits = this.UnshiftedBits << 16 /*0x10*/ - this.BitsLeft;
    }
  }
}
