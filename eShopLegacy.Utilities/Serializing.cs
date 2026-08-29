using System.IO;
using System.Text;
using System.Text.Json;

namespace eShopLegacy.Utilities
{
    // BinaryFormatter removed (insecure deserialization, OWASP A08). Serialization now uses System.Text.Json.
    public class Serializing
    {
        public Stream Serialize<T>(T input)
        {
            var stream = new MemoryStream();
            JsonSerializer.Serialize(stream, input);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public T Deserialize<T>(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return JsonSerializer.Deserialize<T>(stream);
        }
    }
}
