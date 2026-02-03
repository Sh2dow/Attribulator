using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Attribulator.API.Data;
using Attribulator.API.Serialization;
using Attribulator.API.Utils;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;
using VaultLib.Core.Hashing;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Attrib;
using VaultLib.Core.Types.EA.Reflection;
using VaultLib.Core.Utils;
using YamlDotNet.Serialization;

namespace Attribulator.Plugins.YAMLSupport
{
    /// <summary>
    ///     Implements the YAML storage format.
    /// </summary>
    public class YamlStorageFormat : BaseStorageFormat
    {
        private static readonly IDeserializer Deserializer = new DeserializerBuilder().Build();
        private readonly SerializationOptions _serializationOptions;

        private static object UnwrapScalar(object value)
        {
            if (value is Dictionary<object, object> map)
            {
                if (map.TryGetValue("Hash", out var hashValue) || map.TryGetValue("hash", out hashValue))
                    return UnwrapScalar(hashValue);
                if (map.TryGetValue("Value", out var valueValue) || map.TryGetValue("value", out valueValue))
                    return UnwrapScalar(valueValue);
                if (map.TryGetValue("Key", out var keyValue) || map.TryGetValue("key", out keyValue))
                    return UnwrapScalar(keyValue);
                if (map.Count == 1)
                    return UnwrapScalar(map.Values.First());
                foreach (var v in map.Values)
                {
                    var unwrapped = UnwrapScalar(v);
                    if (unwrapped is IConvertible || unwrapped is string)
                        return unwrapped;
                }
            }

            if (value is List<object> list && list.Count == 1)
                return UnwrapScalar(list[0]);

            return value;
        }

        private static BinKey32 ConvertBinKey32(object value)
        {
            var unwrapped = UnwrapScalar(value);
            if (unwrapped is BinKey32 binKey)
                return binKey;
            if (unwrapped is Key32 key32)
                return new BinKey32(key32.Hash);
            if (unwrapped is IConvertible convertible)
                return new BinKey32(Convert.ToUInt32(convertible, CultureInfo.InvariantCulture));

            var text = unwrapped?.ToString();
            if (string.IsNullOrWhiteSpace(text))
                return BinKey32.Zero;
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (uint.TryParse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                        out var hexValue))
                    return new BinKey32(hexValue);
            }

            if (uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                return new BinKey32(intValue);

            return BinKey32.FromString(text);
        }

        private static string ResolveName(Key32 key)
        {
            return HashManager.ResolveVlt(key.Hash) ?? key.ToString();
        }

        private static string ResolveName(VltClass vltClass)
        {
            return ResolveName(vltClass.Key);
        }

        private static string ResolveName(VltClassField field)
        {
            return ResolveName(field.Key);
        }

        private static string ResolveTypeName(VltClassField field)
        {
            return HashManager.ResolveVlt(field.TypeKey.Hash) ?? field.TypeKey.ToString();
        }

        private static string GetShortPath(VltCollection collection)
        {
            return $"{ResolveName(collection.Class)}/{ResolveName(collection.Key)}";
        }

        public YamlStorageFormat(SerializationOptions serializationOptions)
        {
            _serializationOptions = serializationOptions ?? new SerializationOptions();
        }

        public override SerializedDatabaseInfo LoadInfo(string sourceDirectory)
        {
            var deserializer = new DeserializerBuilder().Build();

            using var dbs = new StreamReader(Path.Combine(sourceDirectory, "info.yml"));
            return deserializer.Deserialize<SerializedDatabaseInfo>(dbs);
        }

        public override void Serialize(Database sourceDatabase, string destinationDirectory,
            IEnumerable<LoadedFile> loadedFiles)
        {
            var loadedFileList = loadedFiles.ToList();
            var loadedDatabase = new SerializedDatabaseInfo
            {
                Classes = new List<SerializedDatabaseClass>(),
                Files = new List<SerializedDatabaseFile>(),
                Types = new List<SerializedTypeInfo>(),
                PrimaryVaultName = loadedFileList.SelectMany(f => f.Vaults).First(v => v.IsPrimaryVault).Name
            };

            loadedDatabase.Files.AddRange(loadedFileList.Select(f => new SerializedDatabaseFile
                {Name = f.Name, Group = f.Group, Vaults = f.Vaults.Select(v => v.Name).ToList()}));

            foreach (var databaseType in sourceDatabase.Types)
                loadedDatabase.Types.Add(new SerializedTypeInfo
                {
                    Name = databaseType.Name,
                    Size = databaseType.Size
                });

            foreach (var databaseClass in sourceDatabase.Classes)
            {
                var loadedDatabaseClass = new SerializedDatabaseClass
                {
                    Name = ResolveName(databaseClass),
                    Fields = new List<SerializedDatabaseClassField>()
                };

                loadedDatabaseClass.Fields.AddRange(databaseClass.Fields.Values.Select(field =>
                    new SerializedDatabaseClassField
                    {
                        Name = ResolveName(field),
                        TypeName = ResolveTypeName(field),
                        Alignment = field.Alignment,
                        Flags = field.Flags,
                        MaxCount = field.MaxCount,
                        Size = field.Size,
                        Offset = field.Offset,
                        StaticValue =
                            ConvertDataValueToSerializedValue(destinationDirectory, null, field, field.StaticValue)
                    }));

                loadedDatabase.Classes.Add(loadedDatabaseClass);
            }

            var serializerBuilder = new SerializerBuilder();
            var serializer = serializerBuilder.Build();

            using var sw = new StreamWriter(Path.Combine(destinationDirectory, "info.yml"));
            serializer.Serialize(sw, loadedDatabase);

            foreach (var loadedDatabaseFile in loadedFileList)
            {
                var baseDirectory =
                    Path.Combine(destinationDirectory, loadedDatabaseFile.Group, loadedDatabaseFile.Name);
                Directory.CreateDirectory(baseDirectory);

                foreach (var vault in loadedDatabaseFile.Vaults)
                {
                    var vaultDirectory = Path.Combine(baseDirectory, vault.Name).Trim();
                    Directory.CreateDirectory(vaultDirectory);

                    // Problem: Gameplay data is separated into numerous vaults, so we can't easily construct a proper hierarchy
                    // Solution: Store the name of the parent node instead of having an array of children.

                    foreach (var collectionGroup in sourceDatabase.RowManager.GetCollectionsInVault(vault)
                        .GroupBy(v => ResolveName(v.Class)))
                    {
                        var loadedCollections = new List<SerializedCollection>();
                        AddLoadedCollections(vaultDirectory, loadedCollections, collectionGroup);

                        using var vw = new StreamWriter(Path.Combine(vaultDirectory, collectionGroup.Key + ".yml"));
                        serializer.Serialize(vw, loadedCollections);
                    }
                }
            }
        }

        public override string GetFormatId()
        {
            return "yml";
        }

        public override string GetFormatName()
        {
            return "YAML";
        }

        public override bool CanDeserializeFrom(string sourceDirectory)
        {
            return File.Exists(Path.Combine(sourceDirectory, "info.yml"));
        }

        protected override IEnumerable<string> GetDataFilePaths(string directory)
        {
            return Directory.GetFiles(directory, "*.yml");
        }

        protected override async Task<IEnumerable<SerializedCollection>> LoadDataFileAsync(string path)
        {
            var collections =
                Deserializer.Deserialize<List<SerializedCollection>>(
                    await File.ReadAllTextAsync(path));

            // Fix false null values
            foreach (var loadedCollection in collections)
            {
                loadedCollection.Name ??= "null";

                foreach (var k in loadedCollection.Data.Keys.ToList()
                    .Where(k => loadedCollection.Data[k] == null))
                    loadedCollection.Data[k] = "null";
            }

            return collections;
        }

        private void AddLoadedCollections(string directory, ICollection<SerializedCollection> loadedVaultCollections,
            IEnumerable<VltCollection> vltCollections)
        {
            foreach (var vltCollection in vltCollections)
            {
                var loadedCollection = new SerializedCollection
                {
                    Name = ResolveName(vltCollection.Key),
                    ParentName = vltCollection.Parent != null ? ResolveName(vltCollection.Parent.Key) : null,
                    Data = new Dictionary<string, object>()
                };

                foreach (var (key, value) in vltCollection.GetData())
                {
                    var keyName = ResolveName(key);
                    loadedCollection.Data[keyName] =
                        ConvertDataValueToSerializedValue(directory, vltCollection, vltCollection.Class[key], value);
                }

                loadedVaultCollections.Add(loadedCollection);
            }
        }

        private object ConvertDataValueToSerializedValue(string directory, VltCollection collection,
            VltClassField field, object dataPairValue)
        {
            switch (dataPairValue)
            {
                case IStringValue stringValue:
                    return stringValue.GetString();
                case BaseBlob blob:
                    return ProcessBlob(directory, collection, field, blob);
                case VLTArrayType array:
                {
                    var listType = typeof(List<>);
                    var listGenericType = ResolveType(array.ItemType);
                    var constructedListType = listType.MakeGenericType(listGenericType);
                    var instance = (IList) Activator.CreateInstance(constructedListType);

                    if (instance == null) throw new Exception("Activator.CreateInstance returned null");

                    foreach (var arrayItem in array.Items)
                        instance.Add(listGenericType.IsPrimitive || listGenericType.IsEnum ||
                                     listGenericType == typeof(string)
                            ? ConvertDataValueToSerializedValue(directory, collection, field, arrayItem)
                            : arrayItem);

                    return new SerializedArrayWrapper
                    {
                        Capacity = array.Capacity,
                        Data = instance
                    };
                }
                default:
                    return dataPairValue;
            }
        }

        private object ProcessBlob(string directory, VltCollection collection, VltClassField field, BaseBlob blob)
        {
            if (blob.Data != null && blob.Data.Length > 0)
            {
                var blobDir = Path.Combine(directory, "_blobs");
                Directory.CreateDirectory(blobDir);
                var blobPath = Path.Combine(blobDir,
                    $"{GetShortPath(collection).TrimEnd('/', '\\').Replace('/', '_').Replace('\\', '_')}_{ResolveName(field)}.bin");

                File.WriteAllBytes(blobPath, blob.Data);

                return blobPath.Substring(directory.Length + 1);
            }

            return "";
        }

        private static Type ResolveType(Type type)
        {
            return type;
        }

        protected override object ConvertSerializedValueToDataValue(Database database, string gameId, string dir,
            VltClass vltClass,
            VltClassField field,
            VltCollection vltCollection, object serializedValue, bool createInstance = true)
        {
            //    0. Is it null? Bail out right away.
            //    1. Is it a string? Determine underlying primitive type, and then convert.
            //    2. Is it a list? Ensure we have an array, and then convert all values RECURSIVELY.
            //    3. Is it a dictionary? Convert and set all values RECURSIVELY, ignoring ones that cannot be set at runtime.
            //    4. Are none of those conditions true? Bail out.

            if (serializedValue == null) throw new InvalidDataException("Null serializedValue is NOT PERMITTED!");

            // Create a new data instance
            var instance = createInstance
                ? CreateInstance(database, vltClass, field, vltCollection)
                : CreateInstance(database, vltClass, field, vltCollection);

            return DoValueConversion(database, gameId, dir, vltClass, field, vltCollection, serializedValue, instance);
        }

        private static object CreateInstance(Database database, VltClass vltClass, VltClassField field,
            VltCollection vltCollection)
        {
            var itemType = database.TypeRegistry.ResolveFieldType(field);
            if (field.IsArray)
            {
                return new VLTArrayType(field, itemType);
            }

            return database.TypeRegistry.ConstructTypeInstance(itemType);
        }

        private object DoValueConversion(Database database, string gameId, string dir, VltClass vltClass,
            VltClassField field,
            VltCollection vltCollection,
            object serializedValue, object instance)
        {
            if (instance is VLTArrayType array)
            {
                if (serializedValue is IList list)
                {
                    array.Capacity = (ushort)list.Count;
                    array.Items = new List<object>();
                    foreach (var item in list)
                        array.Items.Add(ConvertArrayItem(database, gameId, dir, vltClass, field, vltCollection, array, item));
                    return array;
                }

                // allow scalar shorthand for single-item arrays
                array.Capacity = 1;
                array.Items = new List<object>
                {
                    ConvertArrayItem(database, gameId, dir, vltClass, field, vltCollection, array, serializedValue)
                };
                return array;
            }

            switch (serializedValue)
            {
                case string str:
                    if (string.Equals(str, "null", StringComparison.OrdinalIgnoreCase))
                    {
                        return instance;
                    }
                    if (string.IsNullOrWhiteSpace(str))
                    {
                        return instance;
                    }

                    switch (instance)
                    {
                        case IStringValue stringValue:
                            stringValue.SetString(str);
                            return instance;
                        case IConvertible:
                            return ValueConversionUtils.DoPrimitiveConversion(instance, str);
                        case BaseBlob blob:
                        {
                            if (string.IsNullOrWhiteSpace(str)) return blob;

                            str = Path.Combine(dir, str);
                            if (!File.Exists(str))
                                throw new InvalidDataException(
                                $"Could not locate blob data file for {GetShortPath(vltCollection)}[{ResolveName(field)}]");

                            blob.Data = File.ReadAllBytes(str);

                            return blob;
                        }
                    }

                    break;
                case Dictionary<object, object> dictionary:
                    return instance is VLTArrayType arrayValue
                        ? DoArrayConversion(database, gameId, dir, vltClass, field, vltCollection, arrayValue, dictionary)
                        : DoDictionaryConversion(database, vltClass, field, vltCollection, instance, dictionary);
            }

            throw new InvalidDataException(
                $"Could not convert serialized value of type {serializedValue.GetType()} " +
                $"for {ResolveName(vltClass)}[{ResolveName(field)}] in {GetShortPath(vltCollection)} " +
                $"(value: {serializedValue}, instance: {instance?.GetType()})");
        }

        private VLTArrayType DoArrayConversion(Database database, string gameId, string dir, VltClass vltClass,
            VltClassField field,
            VltCollection vltCollection, VLTArrayType array, Dictionary<object, object> dictionary)
        {
            var capacity = ushort.Parse(dictionary["Capacity"].ToString()!);
            var rawItemList = (List<object>) dictionary["Data"];
            var allowOverride = _serializationOptions.AllowArraySizeOverride;

            if (capacity < rawItemList.Count)
            {
                if (!allowOverride)
                    throw new InvalidDataException(
                        $"In collection {GetShortPath(vltCollection)}, the capacity of array field [{ResolveName(field)}] ({capacity}) is less than the number of elements in the array ({rawItemList.Count}).");
                capacity = (ushort) rawItemList.Count;
            }
            if (field.MaxCount > 0 && (capacity > field.MaxCount || rawItemList.Count > field.MaxCount))
            {
                if (!allowOverride)
                    throw new InvalidDataException(
                        $"In collection {GetShortPath(vltCollection)}, the size or capacity of array field [{ResolveName(field)}] is greater than the allowed size ({field.MaxCount}).");
            }
            array.Capacity = capacity;
            array.Items = new List<object>();
            foreach (var o in rawItemList)
            {
                array.Items.Add(ConvertArrayItem(database, gameId, dir, vltClass, field, vltCollection, array, o));
            }

            return array;
        }

        private object ConvertArrayItem(Database database, string gameId, string dir, VltClass vltClass,
            VltClassField field, VltCollection vltCollection, VLTArrayType array, object serializedValue)
        {
            var itemType = ResolveType(array.ItemType);

            if (serializedValue == null)
                return database.TypeRegistry.ConstructTypeInstance(itemType);

            if (serializedValue is string str)
            {
                if (string.Equals(str, "null", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(str))
                    return database.TypeRegistry.ConstructTypeInstance(itemType);

                if (itemType == typeof(string))
                    return str;

                if (itemType.IsEnum)
                    return Enum.Parse(itemType, str);

                if (itemType.IsPrimitive)
                {
                    var fixedValue = FixUpValueForComplexObject(str, itemType);
                    return Convert.ChangeType(fixedValue, itemType, CultureInfo.InvariantCulture);
                }
            }

            if (itemType.IsEnum)
            {
                var enumValue = UnwrapScalar(serializedValue);
                return Enum.Parse(itemType, enumValue?.ToString());
            }

            if (itemType.IsPrimitive || itemType == typeof(string))
            {
                object fixedValue = UnwrapScalar(serializedValue);

                if (itemType == typeof(bool))
                {
                    if (fixedValue is bool boolValue)
                        return boolValue;
                    if (fixedValue is IConvertible)
                    {
                        try
                        {
                            return Convert.ToBoolean(fixedValue, CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            // fall through to string handling
                        }
                    }

                    var text = fixedValue?.ToString();
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intBool))
                        return intBool != 0;
                    if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatBool))
                        return Math.Abs(floatBool) > float.Epsilon;
                    if (string.Equals(text, "1", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase))
                        return false;
                    if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                fixedValue = FixUpValueForComplexObject(fixedValue, itemType);
                if (fixedValue is IConvertible)
                    return Convert.ChangeType(fixedValue, itemType, CultureInfo.InvariantCulture);

                try
                {
                    return Convert.ChangeType(fixedValue?.ToString(), itemType, CultureInfo.InvariantCulture);
                }
                catch
                {
                    throw new InvalidDataException(
                        $"Could not convert array item for {ResolveName(vltClass)}[{ResolveName(field)}] in {GetShortPath(vltCollection)} " +
                        $"(value: {serializedValue}, itemType: {itemType})");
                }
            }

            if (serializedValue is Dictionary<object, object> dictionary)
            {
                var instance = itemType.IsSubclassOf(typeof(VLTBaseType))
                    ? database.TypeRegistry.ConstructTypeInstance(itemType)
                    : Activator.CreateInstance(itemType);
                return DoDictionaryConversion(database, vltClass, field, vltCollection, instance, dictionary);
            }

            if (itemType.IsInstanceOfType(serializedValue))
                return serializedValue;

            throw new InvalidDataException(
                $"Could not convert array item for {ResolveName(vltClass)}[{ResolveName(field)}] in {GetShortPath(vltCollection)} " +
                $"(value: {serializedValue}, itemType: {itemType})");
        }

        private static object DoDictionaryConversion(Database database, VltClass vltClass, VltClassField field,
            VltCollection vltCollection, object instance, Dictionary<object, object> dictionary)
        {
            foreach (var (key, value) in dictionary)
            {
                var propName = (string) key;
                var propertyInfo =
                    instance.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);

                if (propertyInfo == null)
                {
                    var fieldInfo = instance.GetType().GetField(propName,
                        BindingFlags.Public | BindingFlags.Instance);
                    if (fieldInfo == null)
                        continue;

                    var fieldType = fieldInfo.FieldType;
                    if (fieldType.IsEnum)
                    {
                        fieldInfo.SetValue(instance, Enum.Parse(fieldType, value.ToString()));
                    }
                    else if (fieldType == typeof(BinKey32))
                    {
                        fieldInfo.SetValue(instance, ConvertBinKey32(value));
                    }
                    else if (fieldType.IsPrimitive || fieldType == typeof(string))
                    {
                        var newValue = FixUpValueForComplexObject(value, fieldType);
                        fieldInfo.SetValue(instance,
                            Convert.ChangeType(newValue, fieldType, CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        fieldInfo.SetValue(instance, value);
                    }

                    continue;
                }

                if (propertyInfo.SetMethod == null || !propertyInfo.SetMethod.IsPublic) continue;

                var propType = propertyInfo.PropertyType;

                if (propType.IsEnum)
                {
                    propertyInfo.SetValue(instance, Enum.Parse(propType, value.ToString()));
                }
                else if (propType == typeof(BinKey32))
                {
                    propertyInfo.SetValue(instance, ConvertBinKey32(value));
                }
                else if (propType.IsPrimitive || propType == typeof(string))
                {
                    var newValue = FixUpValueForComplexObject(value, propType);
                    propertyInfo.SetValue(instance,
                        Convert.ChangeType(newValue, propType, CultureInfo.InvariantCulture));
                }
                else
                {
                    switch (value)
                    {
                        case List<object> objects:
                        {
                            var newList = (IList) Activator.CreateInstance(propType, objects.Count);
                            var elemType = propType.GetElementType() ?? throw new Exception();

                            for (var index = 0; index < objects.Count; index++)
                                if (elemType.IsEnum)
                                {
                                    newList[index] = Enum.Parse(elemType, objects[index].ToString());
                                }
                                else
                                {
                                    if (elemType == typeof(string))
                                    {
                                        newList[index] = objects[index];
                                    }
                                    else
                                    {
                                        var fixedValue = FixUpValueForComplexObject(objects[index], elemType);
                                        var convertedValue =
                                            Convert.ChangeType(fixedValue, elemType, CultureInfo.InvariantCulture);

                                        newList[index] = convertedValue;
                                    }
                                }

                            propertyInfo.SetValue(instance, newList);
                            break;
                        }
                        case Dictionary<object, object> objectDictionary:
                        {
                            var propInstance = propType.IsSubclassOf(typeof(VLTBaseType))
                                ? database.TypeRegistry.ConstructTypeInstance(propType)
                                : Activator.CreateInstance(propType);

                            propertyInfo.SetValue(instance,
                                DoDictionaryConversion(database, vltClass, field, vltCollection, propInstance,
                                    objectDictionary));
                            break;
                        }
                        default:
                        {
                            if (value != null) throw new Exception();

                            break;
                        }
                    }
                }
            }

            return instance;
        }

        private static object FixUpValueForComplexObject(object value, Type elemType)
        {
            if (value is string s)
                if (s.StartsWith("0x", StringComparison.Ordinal) && elemType == typeof(uint))
                    return uint.Parse(s.Substring(2), NumberStyles.AllowHexSpecifier);

            return value;
        }

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public class SerializedArrayWrapper
        {
            public ushort Capacity { get; set; }
            public IList Data { get; set; }
        }
    }
}
