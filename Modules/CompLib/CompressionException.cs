// Decompiled with JetBrains decompiler
// Type: CompLib.CompressionException
// Assembly: CompLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 1C241C1A-9F16-4768-8714-5612B5A64360
// Assembly location: D:\Repos\Games\Attribulator1\Attribulator.CLI\bin\x86\Debug\net9.0-windows\plugins\Attribulator.Plugins.SpeedProfiles\CompLib.dll

using System;
using System.Runtime.Serialization;


namespace CompLib;

[Serializable]
public class CompressionException : Exception
{
  public CompressionException()
  {
  }

  public CompressionException(string message)
    : base(message)
  {
  }

  public CompressionException(string message, Exception inner)
    : base(message, inner)
  {
  }

  protected CompressionException(SerializationInfo info, StreamingContext context)
    : base(info, context)
  {
  }
}
