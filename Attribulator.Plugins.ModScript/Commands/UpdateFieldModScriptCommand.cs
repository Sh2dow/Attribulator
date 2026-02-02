using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Attribulator.API.Utils;
using Attribulator.ModScript.API;
using Attribulator.ModScript.API.Utils;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Attrib.Types;
using VaultLib.Core.Utils;
using VaultLib.Core.Hashing;

namespace Attribulator.Plugins.ModScript.Commands
{
    // update_field class node field [property] value
    public class UpdateFieldModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string FieldName { get; set; }
        public int ArrayIndex { get; set; }
        public List<string> PropertyPath { get; set; }
        public string Value { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count < 5) throw new CommandParseException("Expected at least 5 tokens");

            ClassName = parts[1];
            CollectionName = CleanHashString(parts[2]);
            FieldName = parts[3];
            PropertyPath = new List<string>();

            var split = FieldName.Split(new[] {'[', ']'}, StringSplitOptions.RemoveEmptyEntries);

            switch (split.Length)
            {
                case 2:
                    if (split[1] == "^")
                        ArrayIndex = -1;
                    else
                        ArrayIndex = int.Parse(split[1]);
                    FieldName = split[0];
                    break;
                case 1:
                    FieldName = split[0];
                    break;
                default:
                    throw new CommandParseException("Badly malformed update_field command...");
            }

            FieldName = CleanHashString(FieldName);

            if (parts.Count > 5)
            {
                PropertyPath = parts.Skip(4).Take(parts.Count - 5).ToList();
                Value = parts[^1];
            }
            else
            {
                Value = parts[4];
            }
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);
            var field = GetField(collection.Class, FieldName);
            var data = collection.GetRawValue(field.Key);
            var itemToEdit = data;

            if (data is VLTArrayType array)
            {
                if (ArrayIndex == -1)
                    ArrayIndex = array.Items.Count - 1;
                if (ArrayIndex >= 0 && ArrayIndex < array.Items.Count)
                    itemToEdit = array.Items[ArrayIndex];
                else
                    throw new CommandExecutionException(
                        $"update_field command is out of bounds. Checked: 0 <= {ArrayIndex} < {array.Items.Count}");
            }

            if (PropertyPath.Count == 0)
            {
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
                            var converted = ValueConversionUtils.DoPrimitiveConversion(itemToEdit.GetType(), Value);
                            if (data is VLTArrayType arrayValue)
                            {
                                arrayValue.SetValue(ArrayIndex, converted);
                            }
                            else
                            {
                                collection.SetRawValue(field.Key, converted);
                            }

                            break;
                        }

                        throw new CommandExecutionException(
                            $"cannot handle update for {ResolveName(collection.Class.Key)}[{ResolveName(field.Key)}]");
                }
            }
            else
            {
                // TODO for VaultLib: change Matrix to be multiple floats instead of 1 array
                if (itemToEdit is Matrix matrix && PropertyPath.Count == 1)
                {
                    var matrixPath =
                        PropertyPath[0].Split(new[] {'[', ']'}, StringSplitOptions.RemoveEmptyEntries)[1];
                    var indices = matrixPath.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToArray();
                    if (indices.Length != 2) throw new CommandExecutionException("invalid matrix access");

                    var index = 4 * (indices[0] - 1) + (indices[1] - 1);
                    var parsedValue = float.Parse(Value, CultureInfo.InvariantCulture);
                    SetMatrixValue(ref matrix, index, parsedValue);

                    if (data is VLTArrayType arrayValue)
                        arrayValue.SetValue(ArrayIndex, matrix);
                    else
                        collection.SetRawValue(field.Key, matrix);
                }
                else
                {
                    var parsedProperties = PropertyUtils.ParsePath(PropertyPath).ToList();
                    var retrievedProperty = PropertyUtils.GetProperty((VLTBaseType)itemToEdit, parsedProperties);
                    var retrievedValue = retrievedProperty.GetValue();

                    var value = ValueConversionUtils.DoPrimitiveConversion(retrievedValue, Value);
                    if (value == null) throw new Exception();

                    retrievedProperty.SetValue(value);
                }
            }
        }

        private static void SetMatrixValue(ref Matrix matrix, int index, float value)
        {
            switch (index)
            {
                case 0: matrix.M11 = value; break;
                case 1: matrix.M12 = value; break;
                case 2: matrix.M13 = value; break;
                case 3: matrix.M14 = value; break;
                case 4: matrix.M21 = value; break;
                case 5: matrix.M22 = value; break;
                case 6: matrix.M23 = value; break;
                case 7: matrix.M24 = value; break;
                case 8: matrix.M31 = value; break;
                case 9: matrix.M32 = value; break;
                case 10: matrix.M33 = value; break;
                case 11: matrix.M34 = value; break;
                case 12: matrix.M41 = value; break;
                case 13: matrix.M42 = value; break;
                case 14: matrix.M43 = value; break;
                case 15: matrix.M44 = value; break;
                default: throw new CommandExecutionException("invalid matrix index");
            }
        }

        private static Key32 ParseKey(string name)
        {
            if (name.StartsWith("0x") && uint.TryParse(name.Substring(2),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture, out var hexVal))
            {
                return new Key32(hexVal);
            }

            return Key32.FromString(name);
        }

    }
}
