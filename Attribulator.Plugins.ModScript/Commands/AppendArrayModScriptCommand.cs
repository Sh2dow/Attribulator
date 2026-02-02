using System;
using System.Collections.Generic;
using Attribulator.API.Utils;
using Attribulator.ModScript.API;
using VaultLib.Core;
using VaultLib.Core.Types;
using VaultLib.Core.Utils;
using VaultLib.Core.Hashing;

namespace Attribulator.Plugins.ModScript.Commands
{
    // append_array class node field [value]
    public class AppendArrayModScriptCommand : BaseModScriptCommand
    {
        private bool _hasValue;
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string FieldName { get; set; }
        public string Value { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count < 4) throw new CommandParseException("Expected at least 4 tokens");

            ClassName = parts[1];
            CollectionName = CleanHashString(parts[2]);
            FieldName = CleanHashString(parts[3]);

            if (parts.Count > 4)
            {
                Value = parts[4];
                _hasValue = true;
            }
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);
            var field = GetField(collection.Class, FieldName);

            if (!field.IsArray)
                throw new CommandExecutionException($"Field {ClassName}[{FieldName}] is not an array!");

            if (!collection.HasEntry(FieldName))
                throw new CommandExecutionException(
                    $"Collection {ResolveName(collection.Class.Key)}/{ResolveName(collection.Key)} does not have an entry for {FieldName}.");

            var array = collection.GetRawValue<VLTArrayType>(FieldName);

            if (array.Items.Count == array.Capacity && field.IsInLayout)
                throw new CommandExecutionException("Cannot append to a full array when it is a layout field");

            if (array.Items.Count + 1 > field.MaxCount)
                throw new CommandExecutionException(
                    "Appending to this array would cause it to exceed the maximum number of allowed elements.");

            var itemToEdit = databaseHelper.Database.TypeRegistry.ConstructTypeInstance(array.ItemType);

            if (_hasValue)
                switch (itemToEdit)
                {
                    case IStringValue stringValue:
                        stringValue.SetString(Value);
                        break;
                    case BaseRefSpec<Key32> refSpec:
                        // NOTE: This is a compatibility feature for certain types, such as GCollectionKey, which are technically a RefSpec.
                        refSpec.SetCollectionKey(ParseKey(Value));
                        break;
                    default:
                        if (itemToEdit is IConvertible)
                        {
                            itemToEdit = ValueConversionUtils.DoPrimitiveConversion(itemToEdit.GetType(), Value);
                            break;
                        }

                        throw new CommandExecutionException(
                            $"Object stored in {ResolveName(collection.Class.Key)}[{ResolveName(field.Key)}] is not a simple type and cannot be used in a value-append command");
                }

            array.Items.Add(itemToEdit);

            if (!field.IsInLayout) array.Capacity++;
        }

        private static Key32 ParseKey(string name)
        {
            if (name.StartsWith("0x") && uint.TryParse(name.Substring(2),
                    System.Globalization.NumberStyles.AllowHexSpecifier,
                    System.Globalization.CultureInfo.InvariantCulture, out var hexVal))
            {
                return new Key32(hexVal);
            }

            return Key32.FromString(name);
        }

    }
}
