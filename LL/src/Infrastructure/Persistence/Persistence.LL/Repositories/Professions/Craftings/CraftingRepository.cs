using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Professions.Craftings;
public class CraftingRepository : ICraftingRepository
{
    private readonly IDbContext _dbContext;
    public CraftingRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CraftItemFromRecipeAsync(Guid characterId, Guid recipeId, CancellationToken cancellationToken)
    {
        var recipe = await _dbContext.Recipes
            .Include(r => r.Materials)
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);
        if (recipe == null) return false;

        
        //var test = new InventoryItem
        //{
        //    InventoryId = characterId,
        //    Id = Guid.NewGuid(),
        //    ItemId = recipe.ResultItemId,
        //    Quantity = 1,
        //    CharacterId = characterId
        //});

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}