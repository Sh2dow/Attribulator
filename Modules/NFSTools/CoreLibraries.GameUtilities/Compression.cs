// Decompiled with JetBrains decompiler
// Type: CoreLibraries.GameUtilities.Compression
// Assembly: CoreLibraries.GameUtilities, Version=1.0.4.0, Culture=neutral, PublicKeyToken=null
// MVID: 168AE0E5-B743-4B09-A734-9D8BA0E465C0
// Assembly location: D:\Repos\Games\VaultLib_Boy_Tools\NTS-Tools\dlls\CoreLibraries.GameUtilities.dll

using System;
using System.Runtime.InteropServices;

#nullable disable
namespace CoreLibraries.GameUtilities
{
  public static class Compression
  {
    public static int Decompress(byte[] compressedData, byte[] decompressedData)
    {
      return Compression._internal_decompress(compressedData, compressedData.Length, decompressedData, decompressedData.Length);
    }

    public static int Compress(byte[] uncompressedData, ref byte[] compressedData)
    {
      int newSize = Compression._internal_compress(uncompressedData, uncompressedData.Length, compressedData);
      if (compressedData.Length < newSize)
        throw new Exception();
      Array.Resize<byte>(ref compressedData, newSize);
      return newSize;
    }

    [DllImport("NativeLibrary", EntryPoint = "LZDecompress", CallingConvention = CallingConvention.Cdecl)]
    private static extern int _internal_decompress(
      [In] byte[] inData,
      int inSize,
      [Out] byte[] outData,
      int outSize);

    [DllImport("NativeLibrary", EntryPoint = "JLZCompress", CallingConvention = CallingConvention.Cdecl)]
    private static extern int _internal_compress([In] byte[] inData, int inSize, [Out] byte[] outData);
  }
}
