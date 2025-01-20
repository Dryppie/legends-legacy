namespace Services.LL.Interfaces;
public interface ICreatureService
{
    Task<List<Guid>> GetCreatureIdsByArea(string areaName, CancellationToken cancellationToken);
}