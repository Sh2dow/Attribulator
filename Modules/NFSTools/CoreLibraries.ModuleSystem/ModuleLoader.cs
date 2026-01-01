using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

#nullable disable
namespace CoreLibraries.ModuleSystem
{
  public class ModuleLoader
  {
    private readonly string _searchPattern;
    private readonly string _directory;
    private readonly AggregateCatalog _catalog;

    [ImportMany(typeof (IDataModule))]
    public List<Lazy<IDataModule>> Modules { get; }

    public ModuleLoader(string searchPattern = null, string directory = null)
    {
      this._searchPattern = searchPattern;
      this._directory = directory;
      this._catalog = new AggregateCatalog(new ComposablePartCatalog[1]
      {
        (ComposablePartCatalog) new DirectoryCatalog(this._directory ?? AppDomain.CurrentDomain.BaseDirectory, this._searchPattern ?? "*.dll")
      });
      this.Modules = new List<Lazy<IDataModule>>();
    }

    public void Load()
    {
      new CompositionContainer((ComposablePartCatalog) this._catalog, Array.Empty<ExportProvider>()).ComposeParts((object) this);
      foreach (Lazy<IDataModule> module in this.Modules)
        module.Value.Load();
    }
  }
}
