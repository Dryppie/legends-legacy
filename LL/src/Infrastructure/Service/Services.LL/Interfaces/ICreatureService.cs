namespace Services.LL.Interfaces;
public interface ICreatureService
{
    Task<List<Guid>> GetCreatureIdsByArea(string areaId, CancellationToken cancellationToken);
}