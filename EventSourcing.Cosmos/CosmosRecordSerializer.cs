using System.Text.Json;
using Azure.Core.Serialization;
using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.Cosmos;

internal class CosmosRecordSerializer : CosmosSerializer
{
  private readonly JsonObjectSerializer _serializer = new ();
  private readonly JsonObjectSerializer _recordSerializer;

  private readonly JsonSerializerOptions _options;

  public CosmosRecordSerializer(CosmosRecordStoreOptions options)
  {
    _options = new JsonSerializerOptions
    {
      Converters =
      {
        new RecordConverter<Snapshot>(options.RecordConverterOptions),
        new RecordConverter<Event>(options.RecordConverterOptions),
        new RecordConverter<Projection>(options.RecordConverterOptions)
      }
    };
    
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
    using (stream)
    {
      if (!typeof(T).IsArray || !typeof(T).GetElementType()!.IsSubclassOf(typeof(Projection)))
        return JsonSerializer.Deserialize<T>(stream, _options)!;
    
      var projections = (dynamic) JsonSerializer.Deserialize<Projection[]>(stream, _options)!;
      var result = Array.CreateInstance(typeof(T).GetElementType(), projections.Length);

      for (var i = 0; i < projections.Length; i++)
        result[i] = projections[i];
  
      return result;
    }
  }

  public override Stream ToStream<T>(T input)
  {
    var stream = new MemoryStream();
    _recordSerializer.Serialize(stream, input, typeof(T), default);
    stream.Position = 0;
    return stream;
  }

  private JsonObjectSerializer GetSerializer<T>() =>
    typeof(Record[]).IsAssignableFrom(typeof(T))
      ? _recordSerializer
      : _serializer;
}