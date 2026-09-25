using FixFlow.Application.Common;
using FixFlow.Application.DTOs.Categories;
using FixFlow.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryService categories, IValidator<CategoryWriteRequest> validator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await categories.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await categories.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Create(CategoryWriteRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await categories.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Update(Guid id, CategoryWriteRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await categories.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await categories.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/requirements")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Requirements(Guid id, List<CategoryRequirementWriteRequest> request, CancellationToken cancellationToken) =>
        Ok(await categories.ReplaceRequirementsAsync(id, request, cancellationToken));
}
