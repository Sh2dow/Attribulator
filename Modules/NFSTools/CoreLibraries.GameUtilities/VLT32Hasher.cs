// Decompiled with JetBrains decompiler
// Type: CoreLibraries.GameUtilities.VLT32Hasher
// Assembly: CoreLibraries.GameUtilities, Version=1.0.4.0, Culture=neutral, PublicKeyToken=null
// MVID: 168AE0E5-B743-4B09-A734-9D8BA0E465C0
// Assembly location: D:\Repos\Games\VaultLib_Boy_Tools\NTS-Tools\dlls\CoreLibraries.GameUtilities.dll

using System;
using System.Globalization;

#nullable disable
namespace CoreLibraries.GameUtilities
{
  public static class VLT32Hasher
  {
    public static uint Hash(string k, uint init = 2882400000, bool returnZeroForEmpty = true)
    {
      if (string.IsNullOrEmpty(k) && k == null | returnZeroForEmpty)
        return 0;
      uint result;
      if (k.StartsWith("0x") && uint.TryParse(k.Substring(2), NumberStyles.AllowHexSpecifier, (IFormatProvider) CultureInfo.CurrentCulture, out result))
        return result;
      int index = 0;
      int length = k.Length;
      uint num1 = 2654435769;
      uint num2 = num1;
      uint num3 = init;
      for (; length >= 12; length -= 12)
      {
        uint num4 = num1 + (uint) ((int) k[index] + ((int) k[1 + index] << 8) + ((int) k[2 + index] << 16) + ((int) k[3 + index] << 24));
        uint num5 = num2 + (uint) ((int) k[4 + index] + ((int) k[5 + index] << 8) + ((int) k[6 + index] << 16) + ((int) k[7 + index] << 24));
        uint num6 = num3 + (uint) ((int) k[8 + index] + ((int) k[9 + index] << 8) + ((int) k[10 + index] << 16) + ((int) k[11 + index] << 24));
        uint num7 = num4 - num5 - num6 ^ num6 >> 13;
        uint num8 = num5 - num6 - num7 ^ num7 << 8;
        uint num9 = num6 - num7 - num8 ^ num8 >> 13;
        uint num10 = num7 - num8 - num9 ^ num9 >> 12;
        uint num11 = num8 - num9 - num10 ^ num10 << 16;
        uint num12 = num9 - num10 - num11 ^ num11 >> 5;
        num1 = num10 - num11 - num12 ^ num12 >> 3;
        num2 = num11 - num12 - num1 ^ num1 << 10;
        num3 = num12 - num1 - num2 ^ num2 >> 15;
        index += 12;
      }
      uint num13 = num3 + (uint) k.Length;
      switch (length)
      {
        case 1:
          num1 += (uint) k[index];
          break;
        case 2:
          num1 += (uint) k[1 + index] << 8;
          goto case 1;
        case 3:
          num1 += (uint) k[2 + index] << 16;
          goto case 2;
        case 4:
          num1 += (uint) k[3 + index] << 24;
          goto case 3;
        case 5:
          num2 += (uint) k[4 + index];
          goto case 4;
        case 6:
          num2 += (uint) k[5 + index] << 8;
          goto case 5;
        case 7:
          num2 += (uint) k[6 + index] << 16;
          goto case 6;
        case 8:
          num2 += (uint) k[7 + index] << 24;
          goto case 7;
        case 9:
          num13 += (uint) k[8 + index] << 8;
          goto case 8;
        case 10:
          num13 += (uint) k[9 + index] << 16;
          goto case 9;
        case 11:
          num13 += (uint) k[10 + index] << 24;
          goto case 10;
      }
      uint num14 = num1 - num2 - num13 ^ num13 >> 13;
      uint num15 = num2 - num13 - num14 ^ num14 << 8;
      uint num16 = num13 - num14 - num15 ^ num15 >> 13;
      uint num17 = num14 - num15 - num16 ^ num16 >> 12;
      uint num18 = num15 - num16 - num17 ^ num17 << 16;
      uint num19 = num16 - num17 - num18 ^ num18 >> 5;
      uint num20 = num17 - num18 - num19 ^ num19 >> 3;
      uint num21 = num18 - num19 - num20 ^ num20 << 10;
      return num19 - num20 - num21 ^ num21 >> 15;
    }
  }
}
