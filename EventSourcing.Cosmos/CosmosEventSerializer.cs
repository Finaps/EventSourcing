using System.IO;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;

namespace EventSourcing.Cosmos
{
  internal class CosmosEventSerializer : CosmosSerializer
  {
    private readonly JsonObjectSerializer _serializer;

    public CosmosEventSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
      _serializer = new JsonObjectSerializer(jsonSerializerOptions);
    }

    public override T FromStream<T>(Stream stream)
    {
      using (stream)
      {
        if (stream.CanSeek && stream.Length == 0)
          return default;

        if (typeof(Stream).IsAssignableFrom(typeof(T)))
          return (T)(object)stream;

        return (T)_serializer.Deserialize(stream, typeof(T), default);
      }
    }

    public override Stream ToStream<T>(T input)
    {
      var streamPayload = new MemoryStream();
      _serializer.Serialize(streamPayload, input, typeof(T), default);
      streamPayload.Position = 0;
      return streamPayload;
    }
  }
}