using LogoVisualizer.Api.DTOs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogoVisualizer.Api.Services;

public class MidoceanProductService : IMidoceanProductService
{
    private readonly IReadOnlyList<MidoceanProductDto> _products;

    public MidoceanProductService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "midocean-top10.json");
        using var stream = File.OpenRead(path);
        var file = JsonSerializer.Deserialize<MidoceanDataFile>(stream);
        _products = file?.Products ?? [];
    }

    public IReadOnlyList<MidoceanProductDto> GetAll() => _products;

    public MidoceanProductDto? GetByMasterCode(string masterCode) =>
        _products.FirstOrDefault(p =>
            string.Equals(p.MasterCode, masterCode, StringComparison.OrdinalIgnoreCase));

    // Internal wrapper matching the top-level JSON shape { "products": [...] }
    private sealed class MidoceanDataFile
    {
        [JsonPropertyName("products")]
        public List<MidoceanProductDto> Products { get; init; } = [];
    }
}
