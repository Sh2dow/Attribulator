// Decompiled with JetBrains decompiler
// Type: CoreLibraries.IO.BinaryExtensions
// Assembly: CoreLibraries.IO, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 5B2B526A-D027-4A4F-8773-FFC3B2A757F0
// Assembly location: D:\Repos\Games\VaultLib_Boy_Tools\NTS-Tools\dlls\CoreLibraries.IO.dll

using System;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace CoreLibraries.IO
{
  public static class BinaryExtensions
  {
    public static T[] ReadArray<T>(this BinaryReader binaryReader, Func<T> func, int length)
    {
      T[] objArray = new T[length];
      for (int index = 0; index < length; ++index)
        objArray[index] = func();
      return objArray;
    }

    public static void WriteArray<T>(
      this BinaryWriter binaryWriter,
      IEnumerable<T> array,
      Action<T> writeAction)
    {
      foreach (T obj in array)
        writeAction(obj);
    }

    public static T ReadEnum<T>(this BinaryReader binaryReader) where T : IConvertible
    {
      Type underlyingType = Enum.GetUnderlyingType(typeof (T));
      if (underlyingType == typeof (uint))
        return (T) Enum.ToObject(typeof (T), binaryReader.ReadUInt32());
      if (underlyingType == typeof (int))
        return (T) Enum.ToObject(typeof (T), binaryReader.ReadInt32());
      if (underlyingType == typeof (ushort))
        return (T) Enum.ToObject(typeof (T), binaryReader.ReadUInt16());
      if (underlyingType == typeof (short))
        return (T) Enum.ToObject(typeof (T), binaryReader.ReadInt16());
      throw new Exception();
    }

    public static void WriteEnum<T>(this BinaryWriter binaryWriter, T value) where T : IConvertible
    {
      switch ((IConvertible) Convert.ChangeType((object) value, value.GetTypeCode()))
      {
        case uint num1:
          binaryWriter.Write(num1);
          break;
        case int num2:
          binaryWriter.Write(num2);
          break;
        case ushort num3:
          binaryWriter.Write(num3);
          break;
        case short num4:
          binaryWriter.Write(num4);
          break;
        default:
          throw new Exception();
      }
    }

    public static long WritePointer(this BinaryWriter bw)
    {
      bw.Write(0);
      return bw.BaseStream.Position - 4L;
    }

    public static void AlignReader(this BinaryReader br, int boundary)
    {
      if (br.BaseStream.Position % (long) boundary == 0L)
        return;
      br.BaseStream.Position += (long) boundary - br.BaseStream.Position % (long) boundary;
    }

    public static void AlignReader(this BinaryReader br, uint boundary)
    {
      if (br.BaseStream.Position % (long) boundary == 0L)
        return;
      br.BaseStream.Position += (long) boundary - br.BaseStream.Position % (long) boundary;
    }

    public static void AlignWriter(this BinaryWriter bw, int boundary)
    {
      if (bw.BaseStream.Position % (long) boundary == 0L)
        return;
      long length = (long) boundary - bw.BaseStream.Position % (long) boundary;
      bw.Write(new byte[length]);
    }

    public static void AlignWriter(this BinaryWriter bw, uint boundary)
    {
      if (bw.BaseStream.Position % (long) boundary == 0L)
        return;
      long length = (long) boundary - bw.BaseStream.Position % (long) boundary;
      bw.Write(new byte[length]);
    }
  }
}
