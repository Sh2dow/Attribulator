using System;

namespace Attribulator.API.Serialization
{
    public class SerializationOptions
    {
        // Global fallback for code paths without DI access.
        public static SerializationOptions Current { get; } = new SerializationOptions();

        public bool AllowArraySizeOverride { get; set; }
        public bool AllowDuplicateCollections { get; set; }
        public bool ForceMerge { get; set; }
    }
}
