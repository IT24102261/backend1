using FixFlow.Application.DTOs.Categories;
using FixFlow.Application.Exceptions;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Mapping;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Application.Services;

public class CategoryService(
    IRepository<ServiceCategory> categories,
    IRepository<CategoryVerificationRequirement> requirements,
    IUnitOfWork unitOfWork) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await categories.Query().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var rules = await requirements.Query().ToListAsync(cancellationToken);
        return items.Select(x => Map(x, rules.Where(r => r.CategoryId == x.Id).ToList())).ToList();
    }

    public async Task<CategoryDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");
        var rules = await requirements.Query().Where(x => x.CategoryId == id).ToListAsync(cancellationToken);
        return Map(category, rules);
    }

    public async Task<CategoryDto> CreateAsync(CategoryWriteRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureUniqueName(request.Name, null, cancellationToken);
        var category = new ServiceCategory
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            ParentId = request.ParentId,
            IsActive = request.IsActive
        };
        await categories.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(category, []);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, CategoryWriteRequest request, CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");
        await EnsureUniqueName(request.Name, id, cancellationToken);
        category.Name = request.Name.Trim();
        category.Description = request.Description;
        category.ParentId = request.ParentId;
        category.IsActive = request.IsActive;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var rules = await requirements.Query().Where(x => x.CategoryId == id).ToListAsync(cancellationToken);
        return Map(category, rules);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");
        category.IsActive = false;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryRequirementDto>> ReplaceRequirementsAsync(
        Guid categoryId,
        IReadOnlyList<CategoryRequirementWriteRequest> incoming,
        CancellationToken cancellationToken = default)
    {
        _ = await categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new NotFoundException("Category not found.");
        var existing = await requirements.Query().Where(x => x.CategoryId == categoryId).ToListAsync(cancellationToken);
        foreach (var item in existing)
        {
            requirements.Remove(item);
        }

        foreach (var item in incoming)
        {
            await requirements.AddAsync(new CategoryVerificationRequirement
            {
                CategoryId = categoryId,
                EvidenceType = EnumMap.Parse<EvidenceType>(item.EvidenceType),
                IsRequired = item.IsRequired,
                ValidationMethod = EnumMap.Parse<ValidationMethod>(item.ValidationMethod),
                RuleVersion = string.IsNullOrWhiteSpace(item.RuleVersion) ? "1.0" : item.RuleVersion
            }, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var saved = await requirements.Query().Where(x => x.CategoryId == categoryId).ToListAsync(cancellationToken);
        return saved.Select(MapRequirement).ToList();
    }

    private async Task EnsureUniqueName(string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        var exists = await categories.Query().AnyAsync(
            x => x.Name == name.Trim() && (!excludeId.HasValue || x.Id != excludeId),
            cancellationToken);
        if (exists)
        {
            throw new ConflictException("A category with that name already exists.");
        }
    }

    private static CategoryDto Map(ServiceCategory category, IReadOnlyList<CategoryVerificationRequirement> rules) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        ParentId = category.ParentId,
        IsActive = category.IsActive,
        Requirements = rules.Select(MapRequirement).ToList()
    };

    private static CategoryRequirementDto MapRequirement(CategoryVerificationRequirement requirement) => new()
    {
        Id = requirement.Id,
        EvidenceType = EnumMap.ToApi(requirement.EvidenceType),
        IsRequired = requirement.IsRequired,
        ValidationMethod = EnumMap.ToApi(requirement.ValidationMethod),
        RuleVersion = requirement.RuleVersion
    };
}
