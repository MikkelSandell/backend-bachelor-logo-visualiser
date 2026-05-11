using FluentAssertions;
using LogoVisualizer.Api.Services;
using Xunit;

namespace LogoVisualizer.Tests;

/// <summary>
/// Unit tests for file upload validation.
/// Covers product images (TC-06 to TC-12), logo uploads (TC-34 to TC-41),
/// and product imports (TC-42 to TC-48).
/// </summary>
public class FileUploadValidationTests
{
    private readonly FileValidator _validator = new();

    private const long MaxProductImageSize = 10_485_760; // 10 MB
    private const long MaxLogoSize         = 10_485_760; // 10 MB
    private const long MaxImportFileSize   =  5_242_880; //  5 MB

    // =====================================================================
    // Content type — accepted types
    // =====================================================================

    /// <summary>TC-06/TC-07: Accept PNG and JPEG product images</summary>
    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    public void ValidateContentType_AllowedProductImageType_ReturnsNull(string contentType)
    {
        var error = _validator.ValidateContentType(contentType, ["image/png", "image/jpeg"]);

        error.Should().BeNull();
    }

    /// <summary>TC-34 to TC-36: Accept PNG, JPEG, and SVG logo types</summary>
    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/svg+xml")]
    public void ValidateContentType_AllowedLogoType_ReturnsNull(string contentType)
    {
        var error = _validator.ValidateContentType(contentType, ["image/png", "image/jpeg", "image/svg+xml"]);

        error.Should().BeNull();
    }

    /// <summary>TC-42: Accept application/json for import</summary>
    [Fact]
    public void ValidateContentType_JsonImport_ReturnsNull()
    {
        var error = _validator.ValidateContentType("application/json", ["application/json"]);

        error.Should().BeNull();
    }

    // =====================================================================
    // Content type — rejected types
    // =====================================================================

    /// <summary>TC-08: Reject GIF for product image</summary>
    [Fact]
    public void ValidateContentType_Gif_ReturnsError()
    {
        var error = _validator.ValidateContentType("image/gif", ["image/png", "image/jpeg"]);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("Unsupported");
    }

    /// <summary>TC-37: Reject WEBP for logo</summary>
    [Fact]
    public void ValidateContentType_Webp_ReturnsError()
    {
        var error = _validator.ValidateContentType("image/webp", ["image/png", "image/jpeg", "image/svg+xml"]);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("Unsupported");
    }

    /// <summary>TC-43: Reject plain text for import</summary>
    [Fact]
    public void ValidateContentType_PlainTextForImport_ReturnsError()
    {
        var error = _validator.ValidateContentType("text/plain", ["application/json"]);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("Unsupported");
    }

    /// <summary>Reject null or empty content type regardless of allowed list</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateContentType_NullOrEmpty_ReturnsError(string? contentType)
    {
        var error = _validator.ValidateContentType(contentType, ["image/png"]);

        error.Should().NotBeNullOrEmpty();
    }

    // =====================================================================
    // File size validation — covers all three upload contexts
    // =====================================================================

    // MaxProductImageSize == MaxLogoSize (both 10 MB), so one row covers both contexts.
    // The import limit (5 MB) is tested as a separate row.

    /// <summary>TC-09/TC-38/TC-46: Accept file one byte below the limit</summary>
    [Theory]
    [InlineData(MaxProductImageSize - 1, MaxProductImageSize)]  // covers product image + logo
    [InlineData(MaxImportFileSize - 1,   MaxImportFileSize)]    // import (5 MB limit)
    public void ValidateFileSize_OneByteBelowLimit_ReturnsNull(long fileSize, long limit)
    {
        var error = _validator.ValidateFileSize(fileSize, limit);

        error.Should().BeNull();
    }

    /// <summary>TC-10/TC-39/TC-47: Accept file exactly at the limit</summary>
    [Theory]
    [InlineData(MaxProductImageSize, MaxProductImageSize)]
    [InlineData(MaxImportFileSize,   MaxImportFileSize)]
    public void ValidateFileSize_ExactlyAtLimit_ReturnsNull(long fileSize, long limit)
    {
        var error = _validator.ValidateFileSize(fileSize, limit);

        error.Should().BeNull();
    }

    /// <summary>TC-11/TC-40/TC-48: Reject file one byte above the limit</summary>
    [Theory]
    [InlineData(MaxProductImageSize + 1, MaxProductImageSize)]
    [InlineData(MaxImportFileSize + 1,   MaxImportFileSize)]
    public void ValidateFileSize_OneBytAboveLimit_ReturnsError(long fileSize, long limit)
    {
        var error = _validator.ValidateFileSize(fileSize, limit);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("exceeds");
    }

    // =====================================================================
    // Empty file validation
    // =====================================================================

    /// <summary>TC-12/TC-41/TC-45: Reject empty file (0 bytes)</summary>
    [Fact]
    public void ValidateFileNotEmpty_ZeroBytes_ReturnsError()
    {
        var error = _validator.ValidateFileNotEmpty(0L);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("empty");
    }

    /// <summary>Reject negative size (defensive check)</summary>
    [Fact]
    public void ValidateFileNotEmpty_NegativeSize_ReturnsError()
    {
        var error = _validator.ValidateFileNotEmpty(-1L);

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("empty");
    }

    /// <summary>Accept file with at least one byte</summary>
    [Fact]
    public void ValidateFileNotEmpty_OneByte_ReturnsNull()
    {
        var error = _validator.ValidateFileNotEmpty(1L);

        error.Should().BeNull();
    }

    // =====================================================================
    // File extension validation
    // =====================================================================

    /// <summary>Extension matches content type — accepted cases</summary>
    [Theory]
    [InlineData("logo.png",  "image/png")]
    [InlineData("logo.PNG",  "image/png")]   // case-insensitive
    [InlineData("logo.jpg",  "image/jpeg")]
    [InlineData("logo.jpeg", "image/jpeg")]  // both .jpg and .jpeg are valid for image/jpeg
    [InlineData("logo.svg",  "image/svg+xml")]
    public void ValidateFileExtension_MatchingExtension_ReturnsNull(string fileName, string contentType)
    {
        var error = _validator.ValidateFileExtension(fileName, contentType);

        error.Should().BeNull();
    }

    /// <summary>Extension does not match content type — rejected</summary>
    [Fact]
    public void ValidateFileExtension_PngFileWithJpegType_ReturnsError()
    {
        var error = _validator.ValidateFileExtension("logo.png", "image/jpeg");

        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("extension does not match");
    }

    /// <summary>Null filename skips validation (returns null)</summary>
    [Fact]
    public void ValidateFileExtension_NullFileName_ReturnsNull()
    {
        var error = _validator.ValidateFileExtension(null, "image/png");

        error.Should().BeNull();
    }
}
