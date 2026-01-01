// Decompiled with JetBrains decompiler
// Type: CoreLibraries.GameUtilities.BINHasher
// Assembly: CoreLibraries.GameUtilities, Version=1.0.4.0, Culture=neutral, PublicKeyToken=null
// MVID: 168AE0E5-B743-4B09-A734-9D8BA0E465C0
// Assembly location: D:\Repos\Games\VaultLib_Boy_Tools\NTS-Tools\dlls\CoreLibraries.GameUtilities.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#nullable disable
namespace CoreLibraries.GameUtilities
{
  public static class BINHasher
  {
    public static uint Hash(string text)
    {
      return ((IEnumerable<byte>) Encoding.ASCII.GetBytes(text)).Aggregate<byte, uint>(uint.MaxValue, (Func<uint, byte, uint>) ((h, b) => h * 33U + (uint) b));
    }
  }
}
