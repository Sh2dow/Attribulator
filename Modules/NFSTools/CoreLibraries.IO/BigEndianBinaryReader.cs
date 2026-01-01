// Decompiled with JetBrains decompiler
// Type: CoreLibraries.IO.BigEndianBinaryReader
// Assembly: CoreLibraries.IO, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 5B2B526A-D027-4A4F-8773-FFC3B2A757F0
// Assembly location: D:\Repos\Games\VaultLib_Boy_Tools\NTS-Tools\dlls\CoreLibraries.IO.dll

using System;
using System.IO;
using System.Text;

#nullable disable
namespace CoreLibraries.IO
{
  public class BigEndianBinaryReader : BinaryReader
  {
    public BigEndianBinaryReader(Stream input)
      : base(input)
    {
    }

    public BigEndianBinaryReader(Stream input, Encoding encoding)
      : base(input, encoding)
    {
    }

    public BigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen)
      : base(input, encoding, leaveOpen)
    {
    }

    public override double ReadDouble()
    {
      byte[] numArray = this.ReadBytes(8);
      Array.Reverse((Array) numArray);
      return BitConverter.ToDouble(numArray, 0);
    }

    public override short ReadInt16()
    {
      byte[] numArray = this.ReadBytes(2);
      Array.Reverse((Array) numArray);
      return BitConverter.ToInt16(numArray, 0);
    }

    public override int ReadInt32()
    {
      byte[] numArray = this.ReadBytes(4);
      Array.Reverse((Array) numArray);
      return BitConverter.ToInt32(numArray, 0);
    }

    public override long ReadInt64()
    {
      byte[] numArray = this.ReadBytes(8);
      Array.Reverse((Array) numArray);
      return BitConverter.ToInt64(numArray, 0);
    }

    public override float ReadSingle()
    {
      byte[] numArray = this.ReadBytes(4);
      Array.Reverse((Array) numArray);
      return BitConverter.ToSingle(numArray, 0);
    }

    public override ushort ReadUInt16()
    {
      byte[] numArray = this.ReadBytes(2);
      Array.Reverse((Array) numArray);
      return BitConverter.ToUInt16(numArray, 0);
    }

    public override uint ReadUInt32()
    {
      byte[] numArray = this.ReadBytes(4);
      Array.Reverse((Array) numArray);
      return BitConverter.ToUInt32(numArray, 0);
    }

    public override ulong ReadUInt64()
    {
      byte[] numArray = this.ReadBytes(8);
      Array.Reverse((Array) numArray);
      return BitConverter.ToUInt64(numArray, 0);
    }
  }
}
