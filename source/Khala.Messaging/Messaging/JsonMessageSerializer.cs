namespace Khala.Messaging
{
    using System;
    using System.IO;
    using Newtonsoft.Json;

    public class JsonMessageSerializer : IMessageSerializer
    {
        private readonly JsonSerializer _serializer;

        public JsonMessageSerializer()
        {
            _serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
#if DEBUG
                Formatting = Formatting.Indented
#else
                Formatting = Formatting.None
#endif
            });
        }

        public string Serialize(object message)
        {
            using (var writer = new StringWriter())
            {
                _serializer.Serialize(writer, message);
                return writer.ToString();
            }
        }

        public object Deserialize(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            StringReader reader = null;
            try
            {
                reader = new StringReader(value);
                using (var jsonReader = new JsonTextReader(reader))
                {
                    reader = null;
                    try
                    {
                        return _serializer.Deserialize(jsonReader);
                    }
                    catch (JsonSerializationException)
                    {
                        return JsonConvert.DeserializeObject(value);
                    }
                }
            }
            finally
            {
                reader?.Dispose();
            }
        }
    }
}
