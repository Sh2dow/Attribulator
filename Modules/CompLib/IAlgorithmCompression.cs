// Decompiled with JetBrains decompiler
// Type: CompLib.IAlgorithmCompression
// Assembly: CompLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 1C241C1A-9F16-4768-8714-5612B5A64360
// Assembly location: D:\Repos\Games\Attribulator1\Attribulator.CLI\bin\x86\Debug\net9.0-windows\plugins\Attribulator.Plugins.SpeedProfiles\CompLib.dll

using System;

namespace CompLib;

public interface IAlgorithmCompression
{
  int Compress(ReadOnlySpan<byte> input, Span<byte> output);
}
