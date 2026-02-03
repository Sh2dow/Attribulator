using Attribulator.Plugins.BPSupport.Types;
using VaultLib.Core;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.Exports;
using VaultLib.Core.Exports.Implementations;
using VaultLib.ModernBase.Exports;
using VaultLib.ModernBase.Structures;

namespace Attribulator.Plugins.BPSupport
{
    public class BurnoutParadiseModuleDef : BaseGameModule<Key32>
    {
        public override void RegisterTypes(TypeRegistryBuilder<Key32> typeRegistry)
        {
            typeRegistry.Register<RwVector2>("Attrib::Types::RwVector2");
            typeRegistry.Register<RwVector3>("Attrib::Types::RwVector3");
        }

        public override ExportFactory<Key32> CreateExportFactory()
        {
            return new ExportFactory<Key32>(
                () => new DatabaseLoad(),
                () => new ClassLoad32(),
                () => new CollectionLoad(),
                () => new ExportEntry32());
        }
    }
}
