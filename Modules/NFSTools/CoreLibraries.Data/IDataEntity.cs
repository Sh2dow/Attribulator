using System.Collections.Generic;

namespace CoreLibraries.Data
{
  public interface IDataEntity
  {
    string TypeName { get; }

    string Name { get; }

    ICollection<IDataEntity> GetChildren();

    void AddChild(IDataEntity child);
  }
}
