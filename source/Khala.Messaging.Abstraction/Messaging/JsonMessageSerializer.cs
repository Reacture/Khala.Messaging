namespace Khala.Messaging
{
    using System;
    using System.IO;
    using Newtonsoft.Json;

    /// <summary>
    /// Serializes and deserializes message object into and from <see cref="string"/> in JSON.
    /// </summary>
    public sealed class JsonMessageSerializer : IMessageSerializer
    {
        private readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMessageSerializer"/> class.
        /// </summary>
        public JsonMessageSerializer()
            : this(new JsonSerializerSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMessageSerializer"/> class with a <see cref="JsonSerializerSettings"/> object.
        /// </summary>
        /// <param name="settings">An object contains settings on a <see cref="JsonSerializer"/> object.</param>
        public JsonMessageSerializer(JsonSerializerSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.TypeNameHandling = TypeNameHandling.Objects;
#if DEBUG
            settings.Formatting = Formatting.Indented;
#else
            settings.Formatting = Formatting.None;
#endif

            _serializer = JsonSerializer.Create(settings);
        }

        /// <summary>
        /// Serializes a message object into JSON data.
        /// </summary>
        /// <param name="message">A message object to serialize.</param>
        /// <returns>A <see cref="string"/> that contains serialized JSON data.</returns>
        public string Serialize(object message)
        {
            using (var writer = new StringWriter())
            {
                _serializer.Serialize(writer, message);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Deserialize a message object from JSON data.
        /// </summary>
        /// <param name="value">A <see cref="string"/> that contains serialized JSON data.</param>
        /// <returns>A message object deserialized.</returns>
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
