// Decompiled with JetBrains decompiler
// Type: VaultLib.Frameworks.Speed.SpeedFramework
// Assembly: VaultLib.Frameworks.Speed, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: ED163707-B934-4606-9CEF-4999BD9E877A
// Assembly location: D:\Repos\Games\NFSTools\Attribulator3-alpha4-20250204\plugins\Attribulator.Plugins.SpeedProfiles\VaultLib.Frameworks.Speed.dll

using VaultLib.Core;
using VaultLib.Core.DataInterfaces;

#nullable enable
namespace VaultLib.Frameworks.Speed;

public static class SpeedFramework
{
    /// <summary>
    /// Registers the framework types.
    /// </summary>
    /// <param name="registry">The type registry to register the types with</param>
    public static void Register<TKey>(TypeRegistry<TKey> registry) where TKey : struct, IKey<TKey>
    {
        registry.RegisterAssemblyTypes(typeof(SpeedFramework).Assembly);
    }
}
