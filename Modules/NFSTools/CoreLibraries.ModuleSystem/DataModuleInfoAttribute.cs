// Decompiled with JetBrains decompiler
// Type: CoreLibraries.ModuleSystem.DataModuleInfoAttribute
// Assembly: CoreLibraries.ModuleSystem, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 843BFFAA-DD3F-4481-8FD1-1D223CBACF10
// Assembly location: D:\Repos\Games\VaultLib_Boy_Tools\NTS-Tools\dlls\CoreLibraries.ModuleSystem.dll

using System;
using System.Collections.Generic;
using System.Linq;

#nullable disable
namespace CoreLibraries.ModuleSystem
{
  [AttributeUsage(AttributeTargets.Class)]
  public class DataModuleInfoAttribute : Attribute
  {
    public string Name { get; }

    public List<string> Games { get; }

    public string Author { get; }

    public string Description { get; }

    public DataModuleInfoAttribute(
      string name,
      string author = null,
      string description = null,
      params string[] games)
    {
      this.Name = name;
      this.Games = ((IEnumerable<string>) games).ToList<string>();
      this.Author = author;
      this.Description = description;
    }
  }
}
