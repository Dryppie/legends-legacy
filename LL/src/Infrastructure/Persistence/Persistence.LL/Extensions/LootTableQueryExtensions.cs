using Domain.Models.LootTables;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Extensions;
public static class LootTableQueryExtensions
{
    public static IQueryable<LootTable> IncludeAllEntries(this IQueryable<LootTable> query)
    {
        var test = query
            .Include(lt => lt.Entries)
                .ThenInclude(lt => (lt as LootTable).Entries)
                .ThenInclude(lte => (lte as LootTableItem).Item);
        return test;
    }
}