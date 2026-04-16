using LogoVisualizer.Api.DTOs;

namespace LogoVisualizer.Api.Services;

public interface IMidoceanProductService
{
    IReadOnlyList<MidoceanProductDto> GetAll();
    MidoceanProductDto? GetByMasterCode(string masterCode);

    IReadOnlyList<AdaptedProductDto> GetAllAdapted();
    AdaptedProductDto? GetAdaptedByMasterCode(string masterCode);
}
