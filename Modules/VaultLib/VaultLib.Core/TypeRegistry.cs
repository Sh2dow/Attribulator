using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using CoreLibraries.IO;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;
using VaultLib.Core.Types;
using VaultLib.Core.Utils;

#nullable enable
namespace VaultLib.Core;

public class TypeRegistry<TKey> where TKey : struct, IKey<TKey>
{
    private readonly Database<TKey> _database;
    private readonly Dictionary<(TKey, TKey), Type> _fieldOverrides = new Dictionary<(TKey, TKey), Type>();
    private readonly Dictionary<TKey, Type> _typeDictionary = new Dictionary<TKey, Type>();

    private readonly Dictionary<Type, ObjectActivator<object>> _activators =
        new Dictionary<Type, ObjectActivator<object>>();

    private readonly Dictionary<Type, TypeRegistry<TKey>.TypeReader> _readers =
        new Dictionary<Type, TypeRegistry<TKey>.TypeReader>();

    private readonly Dictionary<Type, TypeRegistry<TKey>.TypeWriter> _writers =
        new Dictionary<Type, TypeRegistry<TKey>.TypeWriter>();

    public ByteOrder ByteOrder { get; }
    
    public TypeRegistry(Database<TKey> database)
    {
        this._database = database;
        this.RegisterAssemblyTypes(typeof(TypeRegistry<>).Assembly);
        this.RegisterPrimitive<bool>("EA::Reflection::Bool", (Func<BinaryReader, bool>)(r => r.ReadByte() > (byte)0),
            (Action<bool, BinaryWriter>)((v, w) => w.Write(v ? (byte)1 : (byte)0)));
        this.RegisterPrimitive<sbyte>("EA::Reflection::Int8", (Func<BinaryReader, sbyte>)(r => r.ReadSByte()),
            (Action<sbyte, BinaryWriter>)((v, w) => w.Write(v)));
        this.RegisterPrimitive<byte>("EA::Reflection::UInt8", (Func<BinaryReader, byte>)(r => r.ReadByte()),
            (Action<byte, BinaryWriter>)((v, w) => w.Write(v)));
        this.RegisterPrimitive<short>("EA::Reflection::Int16", (Func<BinaryReader, short>)(r => r.ReadInt16()),
            (Action<short, BinaryWriter>)((v, w) => w.Write(v)));
        this.RegisterPrimitive<ushort>("EA::Reflection::UInt16", (Func<BinaryReader, ushort>)(r => r.ReadUInt16()),
            (Action<ushort, BinaryWriter>)((v, w) => w.Write(v)));
        this.RegisterPrimitive<int>("EA::Reflection::Int32", (Func<BinaryReader, int>)(r => r.ReadInt32()),
            (Action<int, BinaryWriter>)((v, w) => w.Write(v)));
        this.RegisterPrimitive<uint>("EA::Reflection::UInt32", (Func<BinaryReader, uint>)(r => r.ReadUInt32()),
            (Action<uint, BinaryWriter>)((v, w) => w.Write(v)));
        this.RegisterPrimitive<long>("EA::Reflection::Int64", (Func<BinaryReader, long>)(r => r.ReadInt64()),
            (Action<long, BinaryWriter>)((v, w) => w.Write(v)));
        this.RegisterPrimitive<ulong>("EA::Reflection::UInt64", (Func<BinaryReader, ulong>)(r => r.ReadUInt64()),
            (Action<ulong, BinaryWriter>)((v, w) => w.Write(v)));
        this.RegisterPrimitive<float>("EA::Reflection::Float", (Func<BinaryReader, float>)(r => r.ReadSingle()),
            (Action<float, BinaryWriter>)((v, w) => w.Write(v)));
        this.AddType("EA::Reflection::Text", typeof(string));
        this._activators[typeof(string)] = (ObjectActivator<object>)(_ => (object)string.Empty);
        this._readers[typeof(string)] =
            (TypeRegistry<TKey>.TypeReader)((_1, ctx, _2, br) => (object)ctx.ReadString(br));
        this._writers[typeof(string)] =
            (TypeRegistry<TKey>.TypeWriter)((s, ctx, fieldCtx, bw) => ctx.WriteString((string)s, fieldCtx, bw));
        this.RegisterStruct<Vector2>("Attrib::Types::Vector2");
        this.RegisterStruct<Vector3>("Attrib::Types::Vector3");
        this.RegisterStruct<Vector4>("Attrib::Types::Vector4");
        this.RegisterStruct<Matrix4x4>("Attrib::Types::Matrix");
    }

    private void AddType(string typeName, Type type)
    {
        this._typeDictionary[TKey.FromString(typeName)] = type;
    }

    private void AddFieldOverride(TKey classKey, TKey fieldKey, Type type)
    {
        this._fieldOverrides[(classKey, fieldKey)] = type;
    }

    public void Map<TDest>(string typeId)
    {
        Type type = typeof(TDest);
        if (!this._activators.ContainsKey(type))
            throw new KeyNotFoundException($"Type {type} has not been registered");
        this.AddType(typeId, type);
    }

    public bool IsConstructorRegistered<T>() => this.IsConstructorRegistered(typeof(T));

    public bool IsConstructorRegistered(Type type) => this._activators.ContainsKey(type);

    public void Register<T>(string typeId) where T : VltBaseType<TKey>, new()
    {
        this.RegisterVltBaseType(typeId, typeof(T));
    }

    public void AddFieldOverride<T>(string className, string fieldName) where T : new()
    {
        this.AddFieldOverride<T>(TKey.FromString(className), TKey.FromString(fieldName));
    }

    public void AddFieldOverride<T>(TKey classKey, TKey fieldKey) where T : new()
    {
        this.AddFieldOverride(classKey, fieldKey, typeof(T));
    }

    private void RegisterVltBaseType(string typeId, Type type)
    {
        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor == (ConstructorInfo)null)
            throw new MissingMethodException(
                $"Could not find zero-parameter constructor for type {type} (registered as {typeId})");
        this.AddType(typeId, type);
        this._activators[type] = ReflectionUtils.GetActivator<object>(constructor);
        this._readers[type] = (TypeRegistry<TKey>.TypeReader)((instance, context, fieldContext, reader) =>
        {
            VltBaseType<TKey> vltBaseType = (VltBaseType<TKey>)instance;
            vltBaseType.Read(context, fieldContext, reader);
            return (object)vltBaseType;
        });
        this._writers[type] = (TypeRegistry<TKey>.TypeWriter)((instance, context, fieldContext, writer) =>
            ((VltBaseType<TKey>)instance).Write(context, fieldContext, writer));
    }

    public void RegisterStruct<T>(string typeId) where T : unmanaged
    {
        this.RegisterStruct(typeId, typeof(T));
    }

    private void RegisterStruct(string typeId, Type type)
    {
        if (ByteOrder == ByteOrder.Big)
        {
            if (!typeof(IComplexType).IsAssignableFrom(type))
            {
                throw new Exception(
                    $"Struct type {type} doesn't implement IComplexType, which is necessary for big-endian operations");
            }
        }

        if (!_typeDictionary.ContainsValue(type))
        {
            _activators[type] = _ => Activator.CreateInstance(type)!;
            TypeReader readerProxy = CreateStructReaderProxy(type);
            TypeWriter writerProxy = CreateStructWriterProxy(type);

            if (this.ByteOrder == ByteOrder.Big)
            {
                _readers[type] = (init, context, fieldContext, br) =>
                {
                    var value = readerProxy(init, context, fieldContext, br);
                    ((IComplexType)value).EndianSwap();
                    return value;
                };

                _writers[type] = (value, context, fieldContext, writer) =>
                {
                    // StructWriter writes raw bytes, so we need to do an in-place change
                    ((IComplexType)value).EndianSwap();
                    writerProxy(value, context, fieldContext, writer);
                    // Client might want to use the data after writing it, so we need to be nice
                    // and undo the previous endian swap
                    ((IComplexType)value).EndianSwap();
                };
            }
            else
            {
                _readers[type] = readerProxy;
                _writers[type] = writerProxy;
            }
        }

        AddType(typeId, type);
    }

    private static TypeReader CreateStructReaderProxy(Type structType)
    {
        var paramInitValue = Expression.Parameter(typeof(object), "init");
        var paramContext = Expression.Parameter(typeof(VaultReadContext<TKey>), "context");
        var paramFieldContext = Expression.Parameter(typeof(FieldReadWriteContext<TKey>), "fieldContext");
        var paramBinaryReader = Expression.Parameter(typeof(BinaryReader), "br");

        var body = Expression.Block(
            typeof(object),
            Expression.Convert(Expression.Call(
                    typeof(TypeRegistryBuilder<TKey>), nameof(StructReader), new[] { structType }, paramBinaryReader),
                typeof(object))
        );

        return Expression.Lambda<TypeReader>(body, paramInitValue, paramContext, paramFieldContext,
            paramBinaryReader).Compile();
    }

    private static T StructReader<T>(BinaryReader reader) where T : unmanaged
    {
        Type type = typeof(T);
        int num = Marshal.SizeOf<T>();
        Span<byte> span = stackalloc byte[num];
        if (reader.Read(span) != num)
            throw new EndOfStreamException($"Failed to read {num} bytes for unmanaged type {type}");
        return MemoryMarshal.Read<T>((ReadOnlySpan<byte>)span);
    }

    private static TypeWriter CreateStructWriterProxy(Type structType)
    {
        var paramValue = Expression.Parameter(typeof(object), "value");
        var paramContext = Expression.Parameter(typeof(VaultWriteContext<TKey>), "context");
        var paramFieldContext = Expression.Parameter(typeof(FieldReadWriteContext<TKey>), "fieldContext");
        var paramBinaryWriter = Expression.Parameter(typeof(BinaryWriter), "bw");

        var body = Expression.Call(
            typeof(TypeRegistryBuilder<TKey>), nameof(StructWriter), new[] { structType },
            Expression.Convert(paramValue, structType), paramBinaryWriter);

        return Expression.Lambda<TypeWriter>(body, paramValue, paramContext, paramFieldContext,
            paramBinaryWriter).Compile();
    }

    private static void StructWriter<T>(T value, BinaryWriter writer) where T : unmanaged
    {
        Span<byte> span = stackalloc byte[Marshal.SizeOf<T>()];
        MemoryMarshal.Write<T>(span, ref value);
        writer.Write((ReadOnlySpan<byte>)span);
    }

    public void RegisterPrimitive<T>(
        string typeId,
        Func<BinaryReader, T> reader,
        Action<T, BinaryWriter> writer)
        where T : struct, IConvertible
    {
        Type type = typeof(T);
        this.RegisterPrimitive<T>(typeId, reader, writer, type);
    }

    private void RegisterPrimitive<T>(
        string typeId,
        Func<BinaryReader, T> reader,
        Action<T, BinaryWriter> writer,
        Type type)
        where T : struct, IConvertible
    {
        this.AddType(typeId, type);
        this._activators[type] = (ObjectActivator<object>)(_ => (object)default(T));
        this._readers[type] = (TypeRegistry<TKey>.TypeReader)((_1, _2, _3, r) => (object)reader(r));
        this._writers[type] = (TypeRegistry<TKey>.TypeWriter)((instance, _4, _5, w) => writer((T)instance, w));
    }

    private static Func<BinaryReader, object> CreateEnumReader(Type enumType)
    {
        Type underlyingType = Enum.GetUnderlyingType(enumType);
        if (underlyingType == typeof(uint))
            return (Func<BinaryReader, object>)(r => Enum.ToObject(enumType, r.ReadUInt32()));
        if (underlyingType == typeof(int))
            return (Func<BinaryReader, object>)(r => Enum.ToObject(enumType, r.ReadInt32()));
        if (underlyingType == typeof(ushort))
            return (Func<BinaryReader, object>)(r => Enum.ToObject(enumType, r.ReadUInt16()));
        if (underlyingType == typeof(short))
            return (Func<BinaryReader, object>)(r => Enum.ToObject(enumType, r.ReadInt16()));
        throw new InvalidOperationException("Unsupported enum underlying type: " + underlyingType.FullName);
    }

    private static Action<object, BinaryWriter> CreateEnumWriter(Type enumType)
    {
        Type underlyingType = Enum.GetUnderlyingType(enumType);
        if (underlyingType == typeof(uint))
            return (Action<object, BinaryWriter>)((v, w) => w.Write((uint)v));
        if (underlyingType == typeof(int))
            return (Action<object, BinaryWriter>)((v, w) => w.Write((int)v));
        if (underlyingType == typeof(ushort))
            return (Action<object, BinaryWriter>)((v, w) => w.Write((ushort)v));
        if (underlyingType == typeof(short))
            return (Action<object, BinaryWriter>)((v, w) => w.Write((short)v));
        throw new InvalidOperationException("Unsupported enum underlying type: " + underlyingType.FullName);
    }

    public void RegisterAssemblyTypes(Assembly assembly)
    {
        Debug.WriteLine("RegisterAssemblyTypes({0})", (object)assembly.FullName);
        foreach (Type type in assembly.GetTypes())
        {
            VltTypeInfoAttribute customAttribute = type.GetCustomAttribute<VltTypeInfoAttribute>();
            if (customAttribute == null)
                Debug.WriteLine("DEBUG: skipping registering type {0} because it doesn't have VLTTypeInfo",
                    (object)type.FullName);
            else if (type.IsGenericType || type.IsAbstract || type.IsNested)
                Debug.WriteLine("DEBUG: skipping registering type {0} because it's either generic, abstract, or nested",
                    (object)type.FullName);
            else if (customAttribute.MappedTo != (Type)null)
                this.AddType(customAttribute.Name, customAttribute.MappedTo);
            else if (type.IsEnum)
            {
                this.AddType(customAttribute.Name, type);
                object defaultValue = Activator.CreateInstance(type);
                this._activators[type] = (ObjectActivator<object>)(_ => defaultValue);
                Func<BinaryReader, object> reader = TypeRegistry<TKey>.CreateEnumReader(type);
                Action<object, BinaryWriter> writer = TypeRegistry<TKey>.CreateEnumWriter(type);
                this._readers[type] = (TypeRegistry<TKey>.TypeReader)((_1, _2, _3, r) => reader(r));
                this._writers[type] = (TypeRegistry<TKey>.TypeWriter)((instance, _4, _5, w) => writer(instance, w));
            }
            else if (type.IsValueType)
            {
                if (!TypeRegistry<TKey>.IsUnmanagedType(type))
                    throw new Exception($"Can't register managed struct: {type}");
                this.RegisterStruct(customAttribute.Name, type);
            }
            else if (type.DescendsFrom(typeof(VltBaseType<TKey>)))
                this.RegisterVltBaseType(customAttribute.Name, type);
        }
    }

    private static bool IsUnmanagedType(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type.IsPointer)
            return true;
        return !type.IsGenericType && type.IsValueType &&
               ((IEnumerable<FieldInfo>)type.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                       BindingFlags.NonPublic))
               .All<FieldInfo>((Func<FieldInfo, bool>)(f => TypeRegistry<TKey>.IsUnmanagedType(f.FieldType)));
    }

    public object ConstructTypeInstance(Type type) => this._activators[type]();

    public object ReadFieldValue(
        VaultReadContext<TKey> readContext,
        FieldReadWriteContext<TKey> fieldContext,
        BinaryReader binaryReader)
    {
        VltClassField<TKey> field = fieldContext.Field;
        Type itemType = this.ResolveFieldType(field);
        if (!field.IsArray)
            return this.ReadTypeInstance(readContext, fieldContext, binaryReader);
        VltArrayType<TKey> vltArrayType = new VltArrayType<TKey>(field, itemType);
        vltArrayType.Read(readContext, fieldContext, binaryReader);
        return (object)vltArrayType;
    }

    public object ReadTypeInstance(
        VaultReadContext<TKey> readContext,
        FieldReadWriteContext<TKey> fieldContext,
        BinaryReader binaryReader)
    {
        Type type = this.ResolveFieldType(fieldContext.Field);
        return this.ReadTypeInstance(readContext, fieldContext, binaryReader, type);
    }

    public object ReadTypeInstance(
        VaultReadContext<TKey> readContext,
        FieldReadWriteContext<TKey> fieldContext,
        BinaryReader binaryReader,
        Type type)
    {
        object init = this.ConstructTypeInstance(type);
        return this._readers[type](init, readContext, fieldContext, binaryReader);
    }

    public void WriteFieldValue(
        object instance,
        VaultWriteContext<TKey> writeContext,
        FieldReadWriteContext<TKey> fieldContext,
        BinaryWriter binaryWriter)
    {
        VltClassField<TKey> field = fieldContext.Field;
        if (field.IsArray)
            ((VltBaseType<TKey>)instance).Write(writeContext, fieldContext, binaryWriter);
        else
            this.WriteTypeInstance(field, instance, writeContext, fieldContext, binaryWriter);
    }

    public void WriteTypeInstance(
        VltClassField<TKey> vltClassField,
        object instance,
        VaultWriteContext<TKey> writeContext,
        FieldReadWriteContext<TKey> fieldContext,
        BinaryWriter binaryWriter)
    {
        Type type = this.ResolveFieldType(vltClassField);
        this.WriteTypeInstance(instance, writeContext, fieldContext, binaryWriter, type);
    }

    public void WriteTypeInstance(
        object instance,
        VaultWriteContext<TKey> writeContext,
        FieldReadWriteContext<TKey> fieldContext,
        BinaryWriter binaryWriter,
        Type type)
    {
        Debug.Assert(instance.GetType() == type, "instance.GetType() == type");
        this._writers[type](instance, writeContext, fieldContext, binaryWriter);
    }

    public Type ResolveType(string typeId)
    {
        Type type;
        if (this._typeDictionary.TryGetValue(TKey.FromString(typeId), out type))
            return type;
        throw new KeyNotFoundException($"Type '{typeId}' is not registered");
    }

    public Type ResolveFieldType(VltClassField<TKey> field)
    {
        return this.ResolveFieldType(field.Class.Key, field.Key, field.TypeKey);
    }

    public Type ResolveFieldType(TKey classKey, TKey fieldKey, TKey fieldTypeKey)
    {
        Type type;
        return this._fieldOverrides.TryGetValue((classKey, fieldKey), out type) ? type : this.ResolveType(fieldTypeKey);
    }

    private Type ResolveType(TKey key)
    {
        Type type;
        if (this._typeDictionary.TryGetValue(key, out type))
            return type;
        DatabaseTypeInfo databaseTypeInfo =
            this._database.Types.Find((Predicate<DatabaseTypeInfo>)(t => TKey.FromString(t.Name) == key));
        if (databaseTypeInfo != null)
            throw new KeyNotFoundException($"Type {databaseTypeInfo.Name} (key: {key}) is not registered");
        throw new KeyNotFoundException($"Type {key} is not registered");
    }

    private delegate object TypeReader(
        object init,
        VaultReadContext<TKey> context,
        FieldReadWriteContext<TKey> fieldContext,
        BinaryReader br);

    private delegate void TypeWriter(
        object value,
        VaultWriteContext<TKey> context,
        FieldReadWriteContext<TKey> fieldContext,
        BinaryWriter bw);
}
