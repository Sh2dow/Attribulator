using System;
using System.Collections.Generic;
using System.Linq;

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
