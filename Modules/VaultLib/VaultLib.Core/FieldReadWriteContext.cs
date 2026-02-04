using System.Runtime.CompilerServices;
using System.Text;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;

#nullable enable
namespace VaultLib.Core;

public record FieldReadWriteContext<TKey>(
    VltClass<TKey> Class,
    VltClassField<TKey> Field,
    VltCollection<TKey>? Collection)
    where TKey : struct, IKey<TKey>
{
    public bool IsInVlt
    {
        get => !this.Field.IsInLayout && this.Field.Size <= (ushort) 4 && !this.Field.IsArray;
    }

    [CompilerGenerated]
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();
        builder.Append("Class = ");
        builder.Append((object) this.Class);
        builder.Append(", Field = ");
        builder.Append((object) this.Field);
        builder.Append(", Collection = ");
        builder.Append((object) this.Collection);
        builder.Append(", IsInVlt = ");
        builder.Append(this.IsInVlt.ToString());
        return true;
    }
}