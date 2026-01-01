// Decompiled with JetBrains decompiler
// Type: CoreLibraries.GameUtilities.VLT64Hasher
// Assembly: CoreLibraries.GameUtilities, Version=1.0.4.0, Culture=neutral, PublicKeyToken=null
// MVID: 168AE0E5-B743-4B09-A734-9D8BA0E465C0
// Assembly location: D:\Repos\Games\VaultLib_Boy_Tools\NTS-Tools\dlls\CoreLibraries.GameUtilities.dll

using System;
using System.Globalization;
using System.Text;

#nullable disable
namespace CoreLibraries.GameUtilities
{
  public static class VLT64Hasher
  {
    public static ulong Hash(string str, ulong init = 12379813734277854020, bool returnZeroForEmpty = true)
    {
      if (string.IsNullOrEmpty(str) && str == null | returnZeroForEmpty)
        return 0;
      uint result;
      if (str.StartsWith("0x") && uint.TryParse(str.Substring(2), NumberStyles.AllowHexSpecifier, (IFormatProvider) CultureInfo.CurrentCulture, out result))
        return (ulong) result;
      byte[] bytes = Encoding.ASCII.GetBytes(str);
      uint length = (uint) str.Length;
      ulong num1 = (ulong) length;
      ulong num2 = 11400714819323198483;
      ulong num3 = init;
      int index = 0;
      if (num1 < 24UL)
      {
        num2 = 11400714819323198483UL;
        num3 = init;
      }
      else
      {
        do
        {
          ulong num4 = (ulong) ((long) num3 + (long) bytes[index + 8] + ((long) bytes[index + 15] << 56) + (long) bytes[index + 11] * 16777216L + ((long) bytes[index + 13] << 40) + (long) bytes[index + 9] * 256L + (long) bytes[index + 10] * 65536L + ((long) bytes[index + 12] << 32) + ((long) bytes[index + 14] << 48));
          ulong num5 = (ulong) ((long) num2 + (long) bytes[index + 16] + ((long) bytes[index + 23] << 56) + (long) bytes[index + 19] * 16777216L + ((long) bytes[index + 21] << 40) + (long) bytes[index + 17] * 256L + (long) bytes[index + 18] * 65536L + ((long) bytes[index + 20] << 32) + ((long) bytes[index + 22] << 48));
          ulong num6 = (ulong) ((long) init + (long) bytes[index] + ((long) bytes[index + 7] << 56) + (long) bytes[index + 3] * 16777216L + ((long) bytes[index + 5] << 40) + (long) bytes[index + 1] * 256L + (long) bytes[index + 2] * 65536L + ((long) bytes[index + 4] << 32) + ((long) bytes[index + 6] << 48)) - num4 - num5 ^ num5 >> 43;
          init = (ulong) ((long) num4 - (long) num5 - (long) num6 ^ (long) num6 << 9);
          ulong num7 = num5 - num6 - init ^ init >> 8;
          ulong num8 = num6 - init - num7 ^ num7 >> 38;
          init = (ulong) ((long) init - (long) num7 - (long) num8 ^ (long) num8 << 23);
          ulong num9 = num7 - num8 - init ^ init >> 5;
          ulong num10 = num8 - init - num9 ^ num9 >> 35;
          ulong num11 = (ulong) ((long) init - (long) num9 - (long) num10 ^ (long) num10 << 49);
          ulong num12 = num9 - num10 - num11 ^ num11 >> 11;
          init = num10 - num11 - num12 ^ num12 >> 12;
          num3 = (ulong) ((long) num11 - (long) num12 - (long) init ^ (long) init << 18);
          num2 = num12 - init - num3 ^ num3 >> 22;
          index += 24;
          length -= 24U;
        }
        while (23U < length);
      }
      ulong num13 = num2 + (num1 & (ulong) uint.MaxValue);
      switch (length)
      {
        case 1:
          init += (ulong) bytes[index];
          break;
        case 2:
          init += (ulong) bytes[index + 1] * 256UL;
          goto case 1;
        case 3:
          init += (ulong) bytes[index + 2] * 65536UL;
          goto case 2;
        case 4:
          init += (ulong) bytes[index + 3] * 16777216UL;
          goto case 3;
        case 5:
          init += (ulong) bytes[index + 4] << 32;
          goto case 4;
        case 6:
          init += (ulong) bytes[index + 5] << 40;
          goto case 5;
        case 7:
          init += (ulong) bytes[index + 6] << 48;
          goto case 6;
        case 8:
          init += (ulong) bytes[index + 7] << 56;
          goto case 7;
        case 9:
          num3 += (ulong) bytes[index + 8];
          goto case 8;
        case 10:
          num3 += (ulong) bytes[index + 9] * 256UL;
          goto case 9;
        case 11:
          num3 += (ulong) bytes[index + 10] * 65536UL;
          goto case 10;
        case 12:
          num3 += (ulong) bytes[index + 11] * 16777216UL;
          goto case 11;
        case 13:
          num3 += (ulong) bytes[index + 12] << 32;
          goto case 12;
        case 14:
          num3 += (ulong) bytes[index + 13] << 40;
          goto case 13;
        case 15:
          num3 += (ulong) bytes[index + 14] << 48;
          goto case 14;
        case 16:
          num3 += (ulong) bytes[index + 15] << 56;
          goto case 15;
        case 17:
          num13 += (ulong) bytes[index + 16] * 256UL;
          goto case 16;
        case 18:
          num13 += (ulong) bytes[index + 17] * 65536UL;
          goto case 17;
        case 19:
          num13 += (ulong) bytes[index + 18] * 16777216UL;
          goto case 18;
        case 20:
          num13 += (ulong) bytes[index + 19] << 32;
          goto case 19;
        case 21:
          num13 += (ulong) bytes[index + 20] << 40;
          goto case 20;
        case 22:
          num13 += (ulong) bytes[index + 21] << 48;
          goto case 21;
        case 23:
          num13 += (ulong) bytes[index + 22] << 56;
          goto case 22;
      }
      ulong num14 = init - num3 - num13 ^ num13 >> 43;
      init = (ulong) ((long) num3 - (long) num13 - (long) num14 ^ (long) num14 << 9);
      ulong num15 = num13 - num14 - init ^ init >> 8;
      ulong num16 = num14 - init - num15 ^ num15 >> 38;
      ulong num17 = (ulong) ((long) init - (long) num15 - (long) num16 ^ (long) num16 << 23);
      ulong num18 = num15 - num16 - num17 ^ num17 >> 5;
      ulong num19 = num16 - num17 - num18 ^ num18 >> 35;
      ulong num20 = (ulong) ((long) num17 - (long) num18 - (long) num19 ^ (long) num19 << 49);
      ulong num21 = num18 - num19 - num20 ^ num20 >> 11;
      ulong num22 = num19 - num20 - num21 ^ num21 >> 12;
      ulong num23 = (ulong) ((long) num20 - (long) num21 - (long) num22 ^ (long) num22 << 18);
      return num21 - num22 - num23 ^ num23 >> 22;
    }
  }
}
