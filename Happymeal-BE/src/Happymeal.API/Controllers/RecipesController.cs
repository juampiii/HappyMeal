using Happymeal.Application.Features.Recipes.Commands.PublishRecipe;
using Happymeal.API.Dtos;
using Happymeal.API.Extensions;
using Happymeal.Application.Common.Messaging;
using Happymeal.Application.Features.Recipes;
using Happymeal.Application.Features.Recipes.Commands.CreateRecipe;
using Happymeal.Application.Features.Recipes.Commands.HideRecipe;
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
        var result = await sender.SendAsync(new GetRecipesFeature.Query(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetByIdAsync))]
    [ProducesResponseType<RecipeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(new GetRecipeByIdFeature.Query(id), cancellationToken);
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
            new CreateRecipeFeature.Command(request.Id, request.Name, request.Description, request.Ingredients, request.Steps, request.Servings, request.PreparationMinutes, request.Tags), cancellationToken);

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
            new UpdateRecipeFeature.Command(id, request.Name, request.Description, request.Ingredients, request.Steps, request.Servings, request.PreparationMinutes, request.Tags), cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishAsync(Guid id, CancellationToken cancellationToken)
        => this.ToActionResult(await sender.SendAsync(new PublishRecipeFeature.Command(id), cancellationToken));

    /// <summary>Logically deletes a recipe: hides it from listings while preserving detail references.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HideAsync([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.SendAsync(new HideRecipeFeature.Command(id), cancellationToken);
        return this.ToActionResult(result);
    }
}
