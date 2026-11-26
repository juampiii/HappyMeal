using System.Net;
using System.Net.Http.Json;
using Happymeal.API.Dtos;
using Happymeal.Application.Features.Recipes;
using Happymeal.Application.Common.Interfaces;
using Happymeal.Common.Results;
using Happymeal.Domain.Params;
using Happymeal.Domain.Events;
using Happymeal.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace Happymeal.IntegrationTests.Recipes;
public class RecipeLifecycleTests(HappymealApiFactory factory) : TestBase(factory)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private async Task<RecipeDto> CreateAsync()
    {
        var response=await Client.PostAsJsonAsync("api/recipes",new CreateRecipeRequestDto(null,"Tortilla",Ingredients:[new("Sal",0.5m,"cucharada"),new("Huevos",2,"ud")],Steps:["Mezclar","Cocinar"]),Token);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<RecipeDto>(Token))!;
    }
    [Fact]
    public async Task Lifecycle_WhenPublishedAndHidden_ShouldKeepContentAndOnePublicationEvent()
    {
        var recipe=await CreateAsync();
        (await Client.PutAsJsonAsync($"api/recipes/{recipe.Id}",new UpdateRecipeRequestDto("Tortilla",Ingredients:[new("Sal al gusto")],Steps:["Cocinar","Servir"]),Token)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Client.PostAsync($"api/recipes/{recipe.Id}/publish",null,Token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Client.PostAsync($"api/recipes/{recipe.Id}/publish",null,Token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Client.PutAsJsonAsync($"api/recipes/{recipe.Id}",new UpdateRecipeRequestDto("Other"),Token)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Client.DeleteAsync($"api/recipes/{recipe.Id}",Token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Client.DeleteAsync($"api/recipes/{recipe.Id}",Token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var detail=await Client.GetFromJsonAsync<RecipeDto>($"api/recipes/{recipe.Id}",Token);
        detail!.IsHidden.Should().BeTrue();
        detail.IsPublished.Should().BeTrue();
        detail.Ingredients!.Single().Name.Should().Be("Sal al gusto");
        detail.Steps.Should().Equal("Cocinar","Servir");
        (await Client.GetFromJsonAsync<RecipeDto[]>("api/recipes",Token)).Should().BeEmpty();
        (await QueryAsync(db=>db.OutboxMessages.CountAsync(x=>x.Type==typeof(RecipePublishedDomainEvent).FullName,Token))).Should().Be(1);
    }
    [Fact]
    public async Task CreateAsync_WhenEquivalentNamesRace_ShouldPersistOnlyOne()
    {
        var responses=await Task.WhenAll(Client.PostAsJsonAsync("api/recipes",new CreateRecipeRequestDto(null," S\u00d3PA "),Token),Client.PostAsJsonAsync("api/recipes",new CreateRecipeRequestDto(null,"sopa"),Token));
        responses.Select(x=>x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.Created,HttpStatusCode.Conflict]);
        (await QueryAsync(db=>db.Recipes.CountAsync(Token))).Should().Be(1);
    }
    [Fact]
    public async Task SaveAsync_WhenEditRacesWithPublication_ShouldRollbackEditAndOutbox()
    {
        var recipe=await CreateAsync();
        using var a=Factory.Services.CreateScope();
        using var b=Factory.Services.CreateScope();
        var editWork=a.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var publishWork=b.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var edit=(await editWork.RecipeRepository.GetAsync(recipe.Id,Token))!;
        var publish=(await publishWork.RecipeRepository.GetAsync(recipe.Id,Token))!;
        edit.Update(new RecipeParams("Changed",Ingredients:[new("Changed")],Steps:["Changed"]));
        publish.Publish();
        (await publishWork.TrySaveEntitiesAsync(Token)).IsSuccess.Should().BeTrue();
        (await editWork.TrySaveEntitiesAsync(Token)).Error!.Type.Should().Be(ErrorType.Conflict);
        var saved=await Client.GetFromJsonAsync<RecipeDto>($"api/recipes/{recipe.Id}",Token);
        saved!.Name.Should().Be("Tortilla");
        saved.IsPublished.Should().BeTrue();
        saved.Ingredients!.Select(x=>x.Name).Should().Equal("Sal","Huevos");
        (await QueryAsync(db=>db.OutboxMessages.CountAsync(x=>x.Type==typeof(RecipeUpdatedDomainEvent).FullName,Token))).Should().Be(0);
    }
}
