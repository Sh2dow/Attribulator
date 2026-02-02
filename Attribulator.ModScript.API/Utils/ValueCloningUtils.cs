using System;
using System.Linq;
using System.Reflection;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.Types;
using VaultLib.Core.Types.EA.Reflection;

namespace Attribulator.ModScript.API.Utils
{
    /// <summary>
    ///     Exposes a utility function to clone VLT objects
    /// </summary>
    public static class ValueCloningUtils
    {
        /// <summary>
        ///     Creates a complete copy of the given VLT object.
        /// </summary>
        /// <param name="database">The database to resolve types from.</param>
        /// <param name="originalValue">The object to clone.</param>
        /// <param name="vltClass">The VLT class holding the field.</param>
        /// <param name="vltClassField">The VLT field holding the object.</param>
        /// <param name="vltCollection">The VLT collection.</param>
        /// <returns>A new instance of the object with all properties copied.</returns>
        public static object CloneValue(Database database, object originalValue, VltClass vltClass,
            VltClassField vltClassField,
            VltCollection vltCollection)
        {
            if (originalValue == null)
                return null;

            var registry = database.TypeRegistry;

            if (originalValue is VLTArrayType array)
            {
                var newArray = new VLTArrayType(vltClassField, array.ItemType)
                {
                    Capacity = array.Capacity,
                    Items = array.Items.Select(item =>
                    {
                        if (item is VLTBaseType)
                            return CloneValue(database, item, vltClass, vltClassField, vltCollection);
                        if (item is string s)
                            return new string(s);
                        if (item is Array arr)
                            return arr.Clone();
                        if (item is ICloneable cloneable)
                            return cloneable.Clone();
                        return item;
                    }).ToList()
                };

                return newArray;
            }

            if (originalValue is VLTBaseType vltBaseType)
            {
                var newValue = registry.ConstructTypeInstance(vltBaseType.GetType()) as VLTBaseType;
                return CloneObjectWithReflection(vltBaseType, newValue, registry);
            }

            if (originalValue is string str)
                return new string(str);
            if (originalValue is Array arrayValue)
                return arrayValue.Clone();
            if (originalValue is ICloneable cloneableValue)
                return cloneableValue.Clone();

            return originalValue;
        }

        private static VLTBaseType CloneObjectWithReflection(VLTBaseType originalValue, VLTBaseType newValue,
            TypeRegistry<Key32> registry)
        {
            var properties = originalValue.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod?.IsPublic ?? false)
                .ToArray();

            foreach (var propertyInfo in properties)
            {
                var value = propertyInfo.GetValue(originalValue);

                switch (value)
                {
                    case null:
                        propertyInfo.SetValue(newValue, null);
                        continue;
                    case VLTBaseType vltBaseType:
                        propertyInfo.SetValue(newValue, CloneObjectWithReflection(
                            vltBaseType,
                            (VLTBaseType)registry.ConstructTypeInstance(propertyInfo.PropertyType),
                            registry));
                        break;
                    case string str:
                        propertyInfo.SetValue(newValue, new string(str));
                        break;
                    default:
                        if (propertyInfo.PropertyType.IsPrimitive || propertyInfo.PropertyType.IsEnum)
                            propertyInfo.SetValue(newValue, value);
                        else if (value is Array array)
                            propertyInfo.SetValue(newValue, array.Clone());
                        break;
                }
            }

            return newValue;
        }
    }
}
