using FixFlow.Application.DTOs.Categories;

namespace FixFlow.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CategoryWriteRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateAsync(Guid id, CategoryWriteRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryRequirementDto>> ReplaceRequirementsAsync(Guid categoryId, IReadOnlyList<CategoryRequirementWriteRequest> requirements, CancellationToken cancellationToken = default);
}
