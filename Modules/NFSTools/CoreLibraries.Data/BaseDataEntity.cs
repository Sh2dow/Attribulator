using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CoreLibraries.Data
{
  public abstract class BaseDataEntity : IDataEntity
  {
    protected ObservableCollection<IDataEntity> Children { get; } = new ObservableCollection<IDataEntity>();

    public abstract string TypeName { get; }

    public abstract string Name { get; }

    public virtual ICollection<IDataEntity> GetChildren()
    {
      return (ICollection<IDataEntity>) this.Children;
    }

    public virtual void AddChild(IDataEntity child) => this.Children.Add(child);
  }
}
