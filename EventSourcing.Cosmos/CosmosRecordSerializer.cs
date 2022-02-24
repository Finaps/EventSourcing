using System.IO;
using System.Text.Json;
using Azure.Core.Serialization;
using EventSourcing.Core;

namespace EventSourcing.Cosmos;

internal class CosmosRecordSerializer : CosmosSerializer
{
  private readonly JsonObjectSerializer _serializer = new ();
  private readonly JsonObjectSerializer _recordSerializer;

  public CosmosRecordSerializer(CosmosEventStoreOptions options)
  {
    _recordSerializer = new JsonObjectSerializer(new JsonSerializerOptions
    {
      Converters =
      {
        new RecordConverter<Snapshot>(options.RecordConverterOptions),
        new RecordConverter<Event>(options.RecordConverterOptions),
        new RecordConverter<Projection>(options.RecordConverterOptions)
      }
    });
  }

  public override T FromStream<T>(Stream stream)
  {
    using (stream) return (T) GetSerializer<T>().Deserialize(stream, typeof(T), default)!;
  }

  public override Stream ToStream<T>(T input)
  {
    var stream = new MemoryStream();
    _recordSerializer.Serialize(stream, input, typeof(T), default);
    stream.Position = 0;
    return stream;
  }

  private JsonObjectSerializer GetSerializer<T>() =>
    typeof(T) == typeof(Event[]) || typeof(T) == typeof(Snapshot[])
      ? _recordSerializer
      : _serializer;
}