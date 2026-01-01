// Decompiled with JetBrains decompiler
// Type: CoreLibraries.IO.IBinaryAccess
// Assembly: CoreLibraries.IO, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 5B2B526A-D027-4A4F-8773-FFC3B2A757F0
// Assembly location: D:\Repos\Games\VaultLib_Boy_Tools\NTS-Tools\dlls\CoreLibraries.IO.dll

using System.IO;

#nullable disable
namespace CoreLibraries.IO
{
  public interface IBinaryAccess
  {
    void Read(BinaryReader br);

    void Write(BinaryWriter bw);
  }
}
