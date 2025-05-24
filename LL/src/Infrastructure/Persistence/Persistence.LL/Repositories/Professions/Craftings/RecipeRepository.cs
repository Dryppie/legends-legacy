using Application.Common.Interfaces;
using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Professions.Craftings;
public class RecipeRepository : IRecipeRepository
{
    private readonly IDbContext _dbContext;
    public RecipeRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<Recipe?> GetRecipeByIdAsync(Guid recipeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Recipes
            .Include(r => r.Materials)
            .Include(r => r.Item)
                .ThenInclude(i => i.AttributeModifiers)
            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);
    }
}