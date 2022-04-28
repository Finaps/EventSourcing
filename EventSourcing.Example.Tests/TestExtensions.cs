using System.Text;
using System.Text.Json;

namespace Finaps.EventSourcing.Example.Tests;

public static class TestExtensions
{
    public static HttpContent AsHttpContent<TDto>(this TDto dto) =>
        new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
    
    public static async Task<TDto?> AsDto<TDto>(this HttpResponseMessage response) =>
        JsonSerializer.Deserialize<TDto>(await response.Content.ReadAsStringAsync(), SerializerOptions);
    
    public static async Task<Guid> ToGuid(this HttpResponseMessage response) =>
        (await response.AsDto<GuidResponse>())!.Id;
    
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public class GuidResponse
    {
        public Guid Id { get; set; }
    }
}