// Decompiled with JetBrains decompiler
// Type: CompLib.Algorithms.RefPackAlgorithm
// Assembly: CompLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 1C241C1A-9F16-4768-8714-5612B5A64360
// Assembly location: D:\Repos\Games\Attribulator1\Attribulator.CLI\bin\x86\Debug\net9.0-windows\plugins\Attribulator.Plugins.SpeedProfiles\CompLib.dll

using System;


namespace CompLib.Algorithms;

public class RefPackAlgorithm : IAlgorithmDecompression
{
  internal const uint AlgorithmId = 1263552082;

  public int Decompress(ReadOnlySpan<byte> input, Span<byte> output)
  {
    int num1 = 0;
    int num2 = 0;
    ref ReadOnlySpan<byte> local1 = ref input;
    int index1 = num1;
    int num3 = index1 + 1;
    int num4 = (int) local1[index1] << 8;
    ref ReadOnlySpan<byte> local2 = ref input;
    int index2 = num3;
    int num5 = index2 + 1;
    int num6 = (int) local2[index2];
    int num7 = num4 | num6;
    if ((num7 & 256 /*0x0100*/) == 256 /*0x0100*/)
      throw new CompressionException($"Unsupported RefPack flags: 0x{num7:X}");
    int num8 = num7 & 32768 /*0x8000*/;
    if (true)
      ;
    int num9;
    int num10;
    if (num8 == 32768 /*0x8000*/)
    {
      ref ReadOnlySpan<byte> local3 = ref input;
      int index3 = num5;
      int num11 = index3 + 1;
      int num12 = (int) local3[index3] << 24;
      ref ReadOnlySpan<byte> local4 = ref input;
      int index4 = num11;
      int num13 = index4 + 1;
      int num14 = (int) local4[index4] << 16 /*0x10*/;
      int num15 = num12 | num14;
      ref ReadOnlySpan<byte> local5 = ref input;
      int index5 = num13;
      int num16 = index5 + 1;
      int num17 = (int) local5[index5] << 8;
      int num18 = num15 | num17;
      ref ReadOnlySpan<byte> local6 = ref input;
      int index6 = num16;
      num9 = index6 + 1;
      int num19 = (int) local6[index6];
      num10 = num18 | num19;
    }
    else
    {
      ref ReadOnlySpan<byte> local7 = ref input;
      int index7 = num5;
      int num20 = index7 + 1;
      int num21 = (int) local7[index7] << 16 /*0x10*/;
      ref ReadOnlySpan<byte> local8 = ref input;
      int index8 = num20;
      int num22 = index8 + 1;
      int num23 = (int) local8[index8] << 8;
      int num24 = num21 | num23;
      ref ReadOnlySpan<byte> local9 = ref input;
      int index9 = num22;
      num9 = index9 + 1;
      int num25 = (int) local9[index9];
      num10 = num24 | num25;
    }
    if (true)
      ;
    int num26 = num10;
    if (num26 != output.Length)
      throw new CompressionException($"Expected output buffer to be {num26} bytes long, but it's {output.Length} bytes long");
    bool flag = true;
    while (flag)
    {
      byte num27 = input[num9++];
      int num28 = 0;
      int num29 = 0;
      int num30;
      if (((int) num27 & 128 /*0x80*/) == 0)
      {
        num30 = (int) num27 & 3;
        num28 = (((int) num27 & 28) >> 2) + 3;
        num29 = ((int) num27 & 96 /*0x60*/) << 3 | (int) input[num9++];
      }
      else if (((int) num27 & 64 /*0x40*/) == 0)
      {
        ref ReadOnlySpan<byte> local10 = ref input;
        int index10 = num9;
        int num31 = index10 + 1;
        byte num32 = local10[index10];
        ref ReadOnlySpan<byte> local11 = ref input;
        int index11 = num31;
        num9 = index11 + 1;
        byte num33 = local11[index11];
        num30 = (int) num32 >> 6;
        num28 = ((int) num27 & 63 /*0x3F*/) + 4;
        num29 = ((int) num32 & 63 /*0x3F*/) << 8 | (int) num33;
      }
      else if (((int) num27 & 32 /*0x20*/) == 0)
      {
        ref ReadOnlySpan<byte> local12 = ref input;
        int index12 = num9;
        int num34 = index12 + 1;
        byte num35 = local12[index12];
        ref ReadOnlySpan<byte> local13 = ref input;
        int index13 = num34;
        int num36 = index13 + 1;
        byte num37 = local13[index13];
        ref ReadOnlySpan<byte> local14 = ref input;
        int index14 = num36;
        num9 = index14 + 1;
        byte num38 = local14[index14];
        num30 = (int) num27 & 3;
        num28 = (((int) num27 & 12) >> 2 << 8 | (int) num38) + 5;
        num29 = ((int) num27 & 16 /*0x10*/) >> 4 << 16 /*0x10*/ | (int) num35 << 8 | (int) num37;
      }
      else
      {
        num30 = (((int) num27 & 31 /*0x1F*/) << 2) + 4;
        if (num30 > 112 /*0x70*/)
        {
          num30 = (int) num27 & 3;
          flag = false;
        }
      }
      if (num30 > 0)
      {
        ReadOnlySpan<byte> readOnlySpan1 = input;
        int start1 = num9;
        ReadOnlySpan<byte> readOnlySpan2 = readOnlySpan1.Slice(start1, num9 + num30 - start1);
        ref ReadOnlySpan<byte> local15 = ref readOnlySpan2;
        Span<byte> span = output;
        int start2 = num2;
        Span<byte> destination = span.Slice(start2, num2 + num30 - start2);
        local15.CopyTo(destination);
        num9 += num30;
        num2 += num30;
      }
      if (num28 > 0)
      {
        for (int index15 = 0; index15 < num28; ++index15)
          output[num2 + index15] = output[num2 - num29 - 1 + index15];
        num2 += num28;
      }
    }
    return num2;
  }
}
