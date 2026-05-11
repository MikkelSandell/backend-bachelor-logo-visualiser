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

    // =====================================================================
    // Product Image File Type Validation (TC-06 to TC-08)
    // =====================================================================

    /// <summary>TC-06: Accept PNG product image</summary>
    [Fact]
    public void ValidateContentType_WithPng_ReturnsNull()
    {
        // Arrange
        var contentType = "image/png";
        var allowedTypes = new[] { "image/png", "image/jpeg" };

        // Act
        var error = _validator.ValidateContentType(contentType, allowedTypes);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-07: Accept JPG product image</summary>
    [Fact]
    public void ValidateContentType_WithJpeg_ReturnsNull()
    {
        // Arrange
        var contentType = "image/jpeg";
        var allowedTypes = new[] { "image/png", "image/jpeg" };

        // Act
        var error = _validator.ValidateContentType(contentType, allowedTypes);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-08: Reject unsupported product image (GIF)</summary>
    [Fact]
    public void ValidateContentType_WithGif_ReturnsError()
    {
        // Arrange
        var contentType = "image/gif";
        var allowedTypes = new[] { "image/png", "image/jpeg" };

        // Act
        var error = _validator.ValidateContentType(contentType, allowedTypes);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("Unsupported");
    }

    // =====================================================================
    // Product Image Size Validation (TC-09 to TC-12)
    // =====================================================================

    private const long MaxProductImageSize = 10_485_760; // 10 MB

    /// <summary>TC-09: Accept product image below max size (10MB - 1 byte)</summary>
    [Fact]
    public void ValidateFileSize_JustBelowMaximum_ReturnsNull()
    {
        // Arrange
        var fileSizeBytes = MaxProductImageSize - 1;

        // Act
        var error = _validator.ValidateFileSize(fileSizeBytes, MaxProductImageSize);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-10: Accept product image at max size (10MB)</summary>
    [Fact]
    public void ValidateFileSize_AtMaximum_ReturnsNull()
    {
        // Arrange
        var fileSizeBytes = MaxProductImageSize;

        // Act
        var error = _validator.ValidateFileSize(fileSizeBytes, MaxProductImageSize);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-11: Reject product image above max size (10MB + 1 byte)</summary>
    [Fact]
    public void ValidateFileSize_JustAboveMaximum_ReturnsError()
    {
        // Arrange
        var fileSizeBytes = MaxProductImageSize + 1;

        // Act
        var error = _validator.ValidateFileSize(fileSizeBytes, MaxProductImageSize);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("exceeds");
    }

    /// <summary>TC-12: Reject empty product image file (0 bytes)</summary>
    [Fact]
    public void ValidateFileNotEmpty_WithZeroBytes_ReturnsError()
    {
        // Arrange
        var fileSizeBytes = 0L;

        // Act
        var error = _validator.ValidateFileNotEmpty(fileSizeBytes);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("empty");
    }

    // =====================================================================
    // Logo Upload File Type Validation (TC-34 to TC-37)
    // =====================================================================

    /// <summary>TC-34: Accept PNG logo</summary>
    [Fact]
    public void ValidateContentType_LogoPng_ReturnsNull()
    {
        // Arrange
        var contentType = "image/png";
        var logoAllowedTypes = new[] { "image/png", "image/jpeg", "image/svg+xml" };

        // Act
        var error = _validator.ValidateContentType(contentType, logoAllowedTypes);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-35: Accept JPG logo</summary>
    [Fact]
    public void ValidateContentType_LogoJpeg_ReturnsNull()
    {
        // Arrange
        var contentType = "image/jpeg";
        var logoAllowedTypes = new[] { "image/png", "image/jpeg", "image/svg+xml" };

        // Act
        var error = _validator.ValidateContentType(contentType, logoAllowedTypes);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-36: Accept SVG logo</summary>
    [Fact]
    public void ValidateContentType_LogoSvg_ReturnsNull()
    {
        // Arrange
        var contentType = "image/svg+xml";
        var logoAllowedTypes = new[] { "image/png", "image/jpeg", "image/svg+xml" };

        // Act
        var error = _validator.ValidateContentType(contentType, logoAllowedTypes);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-37: Reject unsupported WEBP logo</summary>
    [Fact]
    public void ValidateContentType_LogoWebp_ReturnsError()
    {
        // Arrange
        var contentType = "image/webp";
        var logoAllowedTypes = new[] { "image/png", "image/jpeg", "image/svg+xml" };

        // Act
        var error = _validator.ValidateContentType(contentType, logoAllowedTypes);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("Unsupported");
    }

    // =====================================================================
    // Logo Upload Size Validation (TC-38 to TC-41)
    // =====================================================================

    private const long MaxLogoSize = 10_485_760; // 10 MB

    /// <summary>TC-38: Accept logo below max size (10MB - 1 byte)</summary>
    [Fact]
    public void ValidateFileSize_LogoBelowMaximum_ReturnsNull()
    {
        // Arrange
        var fileSizeBytes = MaxLogoSize - 1;

        // Act
        var error = _validator.ValidateFileSize(fileSizeBytes, MaxLogoSize);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-39: Accept logo at max size (10MB)</summary>
    [Fact]
    public void ValidateFileSize_LogoAtMaximum_ReturnsNull()
    {
        // Arrange
        var fileSizeBytes = MaxLogoSize;

        // Act
        var error = _validator.ValidateFileSize(fileSizeBytes, MaxLogoSize);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-40: Reject logo above max size (10MB + 1 byte)</summary>
    [Fact]
    public void ValidateFileSize_LogoAboveMaximum_ReturnsError()
    {
        // Arrange
        var fileSizeBytes = MaxLogoSize + 1;

        // Act
        var error = _validator.ValidateFileSize(fileSizeBytes, MaxLogoSize);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("exceeds");
    }

    /// <summary>TC-41: Reject empty logo file (0 bytes)</summary>
    [Fact]
    public void ValidateFileNotEmpty_LogoEmpty_ReturnsError()
    {
        // Arrange
        var fileSizeBytes = 0L;

        // Act
        var error = _validator.ValidateFileNotEmpty(fileSizeBytes);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("empty");
    }

    // =====================================================================
    // Product Import Validation (TC-42 to TC-48)
    // =====================================================================

    private const long MaxImportFileSize = 5_242_880; // 5 MB

    /// <summary>TC-42: Accept valid JSON import file (file extension validated in controller)</summary>
    [Fact]
    public void ValidateContentType_JsonImport_ReturnsNull()
    {
        // Arrange
        var contentType = "application/json";
        var allowedTypes = new[] { "application/json" };

        // Act
        var error = _validator.ValidateContentType(contentType, allowedTypes);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-43: Reject wrong import file extension (tested at controller level - checking JSON content type here)</summary>
    [Fact]
    public void ValidateContentType_NonJsonImport_ReturnsError()
    {
        // Arrange
        var contentType = "text/plain";
        var allowedTypes = new[] { "application/json" };

        // Act
        var error = _validator.ValidateContentType(contentType, allowedTypes);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("Unsupported");
    }

    /// <summary>TC-44: Malformed JSON validation (JSON parsing is tested at controller level, not here)</summary>
    [Fact]
    public void ValidateContentType_JsonFormat_ValidatorDoesNotParseJson()
    {
        // Arrange - Note: This validator only checks content-type, not JSON validity
        // JSON parsing must be tested at controller/service level with actual deserialization
        var contentType = "application/json";

        // Act
        var error = _validator.ValidateContentType(contentType, new[] { "application/json" });

        // Assert
        error.Should().BeNull();  // Validator passes; JSON parsing is separate concern
    }

    /// <summary>TC-45: Reject empty import file (0 bytes)</summary>
    [Fact]
    public void ValidateFileNotEmpty_ImportEmpty_ReturnsError()
    {
        // Arrange
        var fileSizeBytes = 0L;

        // Act
        var error = _validator.ValidateFileNotEmpty(fileSizeBytes);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("empty");
    }

    /// <summary>TC-46: Accept import file below max size (5MB - 1 byte)</summary>
    [Fact]
    public void ValidateFileSize_ImportBelowMaximum_ReturnsNull()
    {
        // Arrange
        var fileSizeBytes = MaxImportFileSize - 1;

        // Act
        var error = _validator.ValidateFileSize(fileSizeBytes, MaxImportFileSize);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-47: Accept import file at max size (5MB)</summary>
    [Fact]
    public void ValidateFileSize_ImportAtMaximum_ReturnsNull()
    {
        // Arrange
        var fileSizeBytes = MaxImportFileSize;

        // Act
        var error = _validator.ValidateFileSize(fileSizeBytes, MaxImportFileSize);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>TC-48: Reject import file above max size (5MB + 1 byte)</summary>
    [Fact]
    public void ValidateFileSize_ImportAboveMaximum_ReturnsError()
    {
        // Arrange
        var fileSizeBytes = MaxImportFileSize + 1;

        // Act
        var error = _validator.ValidateFileSize(fileSizeBytes, MaxImportFileSize);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("exceeds");
    }

    // =====================================================================
    // File Extension Validation
    // =====================================================================

    /// <summary>Validate file extension matches content type</summary>
    [Fact]
    public void ValidateFileExtension_PngFileWithPngType_ReturnsNull()
    {
        // Arrange
        var fileName = "logo.png";
        var contentType = "image/png";

        // Act
        var error = _validator.ValidateFileExtension(fileName, contentType);

        // Assert
        error.Should().BeNull();
    }

    /// <summary>Reject mismatched file extension and content type</summary>
    [Fact]
    public void ValidateFileExtension_PngFileWithJpegType_ReturnsError()
    {
        // Arrange
        var fileName = "logo.png";
        var contentType = "image/jpeg";

        // Act
        var error = _validator.ValidateFileExtension(fileName, contentType);

        // Assert
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("extension does not match");
    }

    /// <summary>Case-insensitive extension validation</summary>
    [Fact]
    public void ValidateFileExtension_UppercaseExtension_ReturnsNull()
    {
        // Arrange
        var fileName = "logo.PNG";
        var contentType = "image/png";

        // Act
        var error = _validator.ValidateFileExtension(fileName, contentType);

        // Assert
        error.Should().BeNull();
    }
}
