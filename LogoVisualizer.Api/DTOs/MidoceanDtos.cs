using System.Text.Json.Serialization;

namespace LogoVisualizer.Api.DTOs;

public record MidoceanProductDto(
    [property: JsonPropertyName("master_code")]     string MasterCode,
    [property: JsonPropertyName("master_id")]       string MasterId,
    [property: JsonPropertyName("item_color_numbers")] List<string> ItemColorNumbers,
    [property: JsonPropertyName("print_manipulation")] string? PrintManipulation,
    [property: JsonPropertyName("print_template")]  string? PrintTemplate,
    [property: JsonPropertyName("printing_positions")] List<MidoceanPrintingPositionDto> PrintingPositions
);

public record MidoceanPrintingPositionDto(
    [property: JsonPropertyName("position_id")]         string PositionId,
    [property: JsonPropertyName("print_size_unit")]     string? PrintSizeUnit,
    [property: JsonPropertyName("max_print_size_height")] double MaxPrintSizeHeight,
    [property: JsonPropertyName("max_print_size_width")]  double MaxPrintSizeWidth,
    [property: JsonPropertyName("rotation")]            double Rotation,
    [property: JsonPropertyName("print_position_type")] string? PrintPositionType,
    [property: JsonPropertyName("printing_techniques")] List<MidoceanPrintingTechniqueDto> PrintingTechniques,
    [property: JsonPropertyName("points")]              List<MidoceanPointDto> Points,
    [property: JsonPropertyName("images")]              List<MidoceanPositionImageDto> Images,
    [property: JsonPropertyName("category")]            string? Category
);

public record MidoceanPrintingTechniqueDto(
    [property: JsonPropertyName("default")]     bool IsDefault,
    [property: JsonPropertyName("id")]          string Id,
    [property: JsonPropertyName("max_colours")] string? MaxColours
);

public record MidoceanPointDto(
    [property: JsonPropertyName("distance_from_left")] int DistanceFromLeft,
    [property: JsonPropertyName("distance_from_top")]  int DistanceFromTop,
    [property: JsonPropertyName("sequence_no")]        int SequenceNo
);

public record MidoceanPositionImageDto(
    [property: JsonPropertyName("print_position_image_blank")]     string? ImageBlank,
    [property: JsonPropertyName("print_position_image_with_area")] string? ImageWithArea,
    [property: JsonPropertyName("variant_color")]                  string? VariantColor
);
