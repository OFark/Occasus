using System.Text.Json;
using System.Text.Json.Nodes;

namespace Occasus.JSONRepository
{
    public class JsonSourceSettings
    {
        public delegate void ActionRef<T>(ref T item);

        public JsonSourceSettings(string fileName)
        {
            FileName = fileName;
        }

        public bool ClearWholeFile { get; set; }
        public string FileName { get; }

        internal JsonDocumentOptions DocumentOptions = new();
        internal JsonNodeOptions NodeOptions = new();
        internal JsonSerializerOptions SerializerOptions = new();
        internal JsonWriterOptions WriterOptions = new();

        public void JsonDocumentOptions(ActionRef<JsonDocumentOptions> options) => options(ref DocumentOptions);
        public void JsonNodeOptions(ActionRef<JsonNodeOptions> options) => options(ref NodeOptions);
        public void JsonSerializerOptions(Action<JsonSerializerOptions> options) => options(SerializerOptions);
        public void JsonWriterOptions(ActionRef<JsonWriterOptions> options) => options(ref WriterOptions);
    }
}
