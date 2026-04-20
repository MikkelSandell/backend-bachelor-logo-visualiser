using LogoVisualizer.Api.DTOs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogoVisualizer.Api.Services;

public class MidoceanProductService : IMidoceanProductService
{
    private readonly IReadOnlyList<MidoceanProductDto> _products;
    private readonly IReadOnlyList<AdaptedProductDto> _adapted;

    public MidoceanProductService(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "Data", "midocean-top10.json");
        using var stream = File.OpenRead(path);
        var file = JsonSerializer.Deserialize<MidoceanDataFile>(stream);
        _products = file?.Products ?? [];
        _adapted  = _products.Select(Adapt).ToList();
    }

    public IReadOnlyList<MidoceanProductDto>  GetAll()           => _products;
    public IReadOnlyList<AdaptedProductDto>   GetAllAdapted()    => _adapted;

    public MidoceanProductDto? GetByMasterCode(string masterCode) =>
        _products.FirstOrDefault(p =>
            string.Equals(p.MasterCode, masterCode, StringComparison.OrdinalIgnoreCase));

    public AdaptedProductDto? GetAdaptedByMasterCode(string masterCode) =>
        _adapted.FirstOrDefault(p =>
            string.Equals(p.Id, masterCode, StringComparison.OrdinalIgnoreCase));

    // -------------------------------------------------------------------------
    // Midocean → Product adapter
    // -------------------------------------------------------------------------

    private static AdaptedProductDto Adapt(MidoceanProductDto p)
    {
        // Use the first position's first blank image as the product image.
        // Midocean print-position images are typically 1000×1000 px.
        var firstPos  = p.PrintingPositions.FirstOrDefault();
        var imageUrl  = firstPos?.Images.FirstOrDefault()?.ImageBlank ?? "";
        const int imageDimension = 1000; // TODO: fetch actual dimensions when needed

        return new AdaptedProductDto(
            Id:         p.MasterCode,
            Title:      p.MasterCode,
            ImageUrl:   imageUrl,
            ImageWidth: imageDimension,
            ImageHeight: imageDimension,
            PrintZones: p.PrintingPositions.Select(AdaptZone).ToList()
        );
    }

    private static AdaptedPrintZoneDto AdaptZone(MidoceanPrintingPositionDto pos)
    {
        // Points are sorted by sequence_no; point 1 = top-left, point 2 = bottom-right.
        var pts = pos.Points.OrderBy(pt => pt.SequenceNo).ToList();
        int x = 0, y = 0, w = 0, h = 0;
        if (pts.Count >= 2)
        {
            x = pts[0].DistanceFromLeft;
            y = pts[0].DistanceFromTop;
            w = pts[1].DistanceFromLeft - x;
            h = pts[1].DistanceFromTop  - y;
        }

        var techniques = pos.PrintingTechniques
            .Select(t => MapTechnique(t.Id))
            .OfType<string>()
            .Distinct()
            .ToList();

        var maxColors = pos.PrintingTechniques
            .Select(t => int.TryParse(t.MaxColours, out var c) ? c : 0)
            .DefaultIfEmpty(0)
            .Max();

        var imageUrl = pos.Images.FirstOrDefault()?.ImageBlank ?? "";

        return new AdaptedPrintZoneDto(
            Id:                  pos.PositionId,
            Name:                pos.PositionId,
            X: x, Y: y, Width: w, Height: h,
            MaxPhysicalWidthMm:  pos.MaxPrintSizeWidth,
            MaxPhysicalHeightMm: pos.MaxPrintSizeHeight,
            AllowedTechniques:   techniques.Count > 0 ? techniques : ["digital_print"],
            MaxColors:           maxColors,
            ImageUrl:            imageUrl
        );
    }

    /// Maps Midocean technique codes to the frontend's PrintTechnique enum strings.
    private static string? MapTechnique(string code) => code.ToUpperInvariant() switch
    {
        "SP" or "TR" or "ST1" or "ST2" or "SC" or "TS" => "screen_print",
        "E"  or "EM" or "EMB"                           => "embroidery",
        "EN" or "B"  or "LA"  or "LAS" or "ENG"        => "engraving",
        "SL" or "SA" or "SUB" or "DS"                   => "sublimation",
        "DTG" or "TDT" or "TT" or "DP" or "DIG" or "DC" => "digital_print",
        "TP" or "P"  or "PAD" or "PP"                   => "pad_print",
        _ => null   // unknown codes are silently dropped
    };

    // Internal wrapper matching the top-level JSON shape { "products": [...] }
    private sealed class MidoceanDataFile
    {
        [JsonPropertyName("products")]
        public List<MidoceanProductDto> Products { get; init; } = [];
    }
}
