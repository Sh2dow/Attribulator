using System;
using Attribulator.API.Utils;
using VaultLib.Core.DataInterfaces;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

public class VltKeyTypeConverter<TKey> : IYamlTypeConverter where TKey : struct, IKey<TKey>
{
    public bool Accepts(Type type)
    {
        return type == typeof(TKey);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.Accept<Scalar>())
        {
            var scalar = parser.Consume<Scalar>();
            return KeyUtils.StringToKey<TKey>(scalar.Value, true);
        }

        if (parser.Accept<MappingStart>())
        {
            parser.Consume<MappingStart>();
            string? keyValue = null;

            while (!parser.Accept<MappingEnd>())
            {
                var key = parser.Consume<Scalar>().Value;

                if (parser.Accept<Scalar>())
                {
                    var value = parser.Consume<Scalar>().Value;
                    if (key.Equals("Hash", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("Value", StringComparison.OrdinalIgnoreCase))
                    {
                        keyValue = value;
                    }
                }
                else
                {
                    // Skip unexpected nested structures while preserving parser state.
                    parser.SkipThisAndNestedEvents();
                }
            }

            parser.Consume<MappingEnd>();

            if (string.IsNullOrWhiteSpace(keyValue))
            {
                throw new InvalidOperationException("Invalid key mapping. Expected 'Hash' or 'Value' field.");
            }

            return KeyUtils.StringToKey<TKey>(keyValue, true);
        }

        throw new InvalidOperationException($"Unsupported YAML node for {typeof(TKey).Name}.");
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var key = (TKey)value!;
        emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, KeyUtils.KeyToString(key),
            ScalarStyle.DoubleQuoted, isPlainImplicit: false, isQuotedImplicit: true));
    }
}