using Happymeal.API.Dtos;
using Happymeal.API.Extensions;
using Happymeal.Application.Common.Messaging;
using Happymeal.Application.Features.Recipes;
using Happymeal.Application.Features.Recipes.Commands.CreateRecipe;
using Happymeal.Application.Features.Recipes.Commands.DeleteRecipe;
using Happymeal.Application.Features.Recipes.Commands.UpdateRecipe;
using Happymeal.Application.Features.Recipes.Queries.GetRecipeById;
using Happymeal.Application.Features.Recipes.Queries.GetRecipes;
using Microsoft.AspNetCore.Mvc;

namespace Happymeal.API.Controllers;

[ApiController]
[Route("api/recipes")]
public sealed class RecipesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RecipeDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(new GetRecipesQuery(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetByIdAsync))]
    [ProducesResponseType<RecipeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(new GetRecipeByIdQuery(id), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType<RecipeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateRecipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(
            new CreateRecipeFeature.Command(request.Id, request.Name, request.Description), cancellationToken);

        return this.ToActionResult(result, dto => CreatedAtAction(nameof(GetByIdAsync), new { id = dto.Id }, dto));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<RecipeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateRecipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(
            new UpdateRecipeFeature.Command(id, request.Name, request.Description), cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(new DeleteRecipeFeature.Command(id), cancellationToken);
        return this.ToActionResult(result);
    }
}
