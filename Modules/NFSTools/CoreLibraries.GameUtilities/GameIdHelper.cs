// Decompiled with JetBrains decompiler
// Type: CoreLibraries.GameUtilities.GameIdHelper
// Assembly: CoreLibraries.GameUtilities, Version=1.0.4.0, Culture=neutral, PublicKeyToken=null
// MVID: 168AE0E5-B743-4B09-A734-9D8BA0E465C0
// Assembly location: D:\Repos\Games\VaultLib_Boy_Tools\NTS-Tools\dlls\CoreLibraries.GameUtilities.dll

using System.Collections.Generic;
using System.Data;

#nullable disable
namespace CoreLibraries.GameUtilities
{
  public static class GameIdHelper
  {
    public const string ID_MW = "MOST_WANTED";
    public const string ID_MW_64 = "MOST_WANTED_64";
    public const string ID_CARBON = "CARBON";
    public const string ID_PROSTREET = "PROSTREET";
    public const string ID_UNDERCOVER = "UNDERCOVER";
    public const string ID_WORLD = "WORLD";
    private static readonly ISet<string> IdList = (ISet<string>) new HashSet<string>()
    {
      "MOST_WANTED",
      "MOST_WANTED_64",
      "CARBON",
      "PROSTREET",
      "UNDERCOVER",
      "WORLD"
    };
    private static readonly Dictionary<string, ISet<string>> FeatureDictionary = new Dictionary<string, ISet<string>>();

    public static void AddFeature(string gameId, string featureId)
    {
      if (!GameIdHelper.FeatureDictionary.ContainsKey(gameId))
        GameIdHelper.FeatureDictionary[gameId] = (ISet<string>) new HashSet<string>();
      GameIdHelper.FeatureDictionary[gameId].Add(featureId);
    }

    public static bool IsFeatureAvailable(string gameId, string featureId)
    {
      ISet<string> stringSet;
      return GameIdHelper.FeatureDictionary.TryGetValue(gameId, out stringSet) && stringSet.Contains(featureId);
    }

    public static void AddGame(string gameId)
    {
      if (!GameIdHelper.IdList.Add(gameId))
        throw new DuplicateNameException(gameId + " already added");
    }

    public static bool IsValid(string gameId) => GameIdHelper.IdList.Contains(gameId);

    public static ISet<string> GetIdList() => GameIdHelper.IdList;
  }
}
