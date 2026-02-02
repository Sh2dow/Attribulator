using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Attribulator.API.Exceptions;
using VaultLib.Core.Hashing;

namespace Attribulator.API.Utils
{
    public static class ValueConversionUtils
    {
        private static readonly Dictionary<Type, Type> TypeCache = new Dictionary<Type, Type>();

        public static object DoPrimitiveConversion(Type conversionType, string str)
        {
            if (conversionType.IsEnum)
            {
                if (str.StartsWith("0x", StringComparison.Ordinal) &&
                    uint.TryParse(str.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture,
                        out var val))
                    return Enum.Parse(conversionType, val.ToString());

                return Enum.Parse(conversionType, str);
            }

            if (str.StartsWith("0x", StringComparison.Ordinal) && uint.TryParse(str.Substring(2),
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture, out var hexVal))
                return Convert.ChangeType(hexVal, conversionType, CultureInfo.InvariantCulture);

            try
            {
                return Convert.ChangeType(str, conversionType, CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                throw new ValueConversionException($"Failed to parse value [{str}] as type {conversionType}", e);
            }
        }

        public static object DoPrimitiveConversion(object value, string str)
        {
            if (value == null)
                // we don't know the type, just assume we need a string
                return str;

            var type = value.GetType();

            if (type == typeof(uint))
            {
                if (str.StartsWith("0x", StringComparison.Ordinal))
                    return uint.Parse(str.Substring(2), NumberStyles.AllowHexSpecifier);
                if (!uint.TryParse(str, out _))
                    return Vlt32Hasher.Hash(str);
            }
            else if (type == typeof(int))
            {
                if (str.StartsWith("0x", StringComparison.Ordinal))
                    return int.Parse(str.Substring(2), NumberStyles.AllowHexSpecifier);
                if (!uint.TryParse(str, out _))
                    return unchecked((int) Vlt32Hasher.Hash(str));
            }

            return DoPrimitiveConversion(type, str);
        }
    }
}
