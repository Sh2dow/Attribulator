// Decompiled with JetBrains decompiler
// Type: CoreLibraries.IO.NullTerminatedString
// Assembly: CoreLibraries.IO, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 5B2B526A-D027-4A4F-8773-FFC3B2A757F0
// Assembly location: D:\Repos\Games\VaultLib_Boy_Tools\NTS-Tools\dlls\CoreLibraries.IO.dll

using System.IO;
using System.Text;

#nullable disable
namespace CoreLibraries.IO
{
  public static class NullTerminatedString
  {
    public static string Read(BinaryReader br) => NullTerminatedString.Read(br.BaseStream);

    public static string Read(Stream stream)
    {
      StringBuilder stringBuilder = new StringBuilder();
      byte num1;
      do
      {
        int num2 = stream.ReadByte();
        if (num2 != -1)
        {
          num1 = (byte) num2;
          if (num1 != (byte) 0)
            stringBuilder.Append((char) num1);
        }
        else
          break;
      }
      while (num1 != (byte) 0);
      return stringBuilder.ToString();
    }

    public static void Write(BinaryWriter bw, string value)
    {
      byte[] bytes = Encoding.ASCII.GetBytes(value);
      bw.Write(bytes);
      bw.Write((byte) 0);
    }
  }
}
