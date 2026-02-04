using System.Runtime.InteropServices;

namespace CompLib;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct CipHeader
{
  public uint CompressBlockMagic;
  public int USize;
  public int CSize;
  public int UPos;
  public int CPos;
  public int Null;
}
