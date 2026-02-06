using System.IO;
using System.Reflection;
using Attribulator.API.Plugin;
using Attribulator.API.Utils;
using VaultLib.Core.Hashing;

namespace Attribulator.Plugins.SpeedProfiles
{
    public class SpeedProfilesPlugin : IPlugin
    {
        public string GetName()
        {
            return "Speed Profiles";
        }

        public void Init()
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var resourcesDir = Path.Combine(assemblyDir, "Resources");
            Directory.CreateDirectory(resourcesDir);

            var hashesPath = Path.Combine(resourcesDir, "hashes.txt");
            var hashesBinPath = Path.Combine(resourcesDir, "hashes_bin.txt");

            EnsureResourceFile(hashesPath, "Attribulator.Plugins.SpeedProfiles.Resources.hashes.txt");
            EnsureResourceFile(hashesBinPath, "Attribulator.Plugins.SpeedProfiles.Resources.hashes_bin.txt");

            HashManager.LoadDictionary(hashesPath);
            KeyUtils.LoadBinDictionary(hashesBinPath);
        }

        private static void EnsureResourceFile(string targetPath, string resourceName)
        {
            if (File.Exists(targetPath))
                return;

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null)
                return;

            using var file = File.Create(targetPath);
            stream.CopyTo(file);
        }
    }
}
