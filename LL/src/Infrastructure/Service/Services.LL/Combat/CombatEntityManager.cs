using Domain.Interfaces.Combat;
using Domain.Models.Entities;

namespace Services.LL.Combat;
public class CombatEntityManager : ICombatEntityManager
{
    private readonly List<Entity> _playerTeam;
    private readonly List<Entity> _enemyTeam;

    public CombatEntityManager(List<Entity> playerTeam, List<Entity> enemyTeam)
    {
        _playerTeam = new List<Entity>(playerTeam);
        _enemyTeam = new List<Entity>(enemyTeam);
    }

    public List<Entity> PlayerTeam => _playerTeam;
    public List<Entity> EnemyTeam => _enemyTeam;
    public List<Entity> AllEntities => _playerTeam.Concat(_enemyTeam).ToList();

    public void AddEntityToOwnTeam(Entity self, Entity entityToAdd)
    {
        if (_playerTeam.Contains(self))
        {
            _playerTeam.Add(entityToAdd);
        }
        else if (_enemyTeam.Contains(self))
        {
            _enemyTeam.Add(entityToAdd);
        }
        else
        {
            throw new InvalidOperationException("Caster not found on any team.");
        }
    }

    public void RemoveEntity(Entity entity)
    {
        _playerTeam.Remove(entity);
        _enemyTeam.Remove(entity);
    }

    public List<Entity> GetOpposingTeam(Entity entity)
    {
        return _playerTeam.Contains(entity) ? _enemyTeam : _playerTeam;
    }

    public List<Entity> GetOwnTeam(Entity entity)
    {
        return _playerTeam.Contains(entity) ? _playerTeam : _enemyTeam;
    }

    public bool IsCombatActive()
    {
        // Check if at least one team still has alive entities
        bool playersAlive = _playerTeam.Any(e => e.IsAlive);
        bool enemiesAlive = _enemyTeam.Any(e => e.IsAlive);
        return playersAlive && enemiesAlive;
    }

    // Optional: You could add convenience methods here:
    // For example:
    // - Get all alive entities on a specific team
    // - Get a random alive target from the opposing team
    // - Reset entity states at the start of combat
    // etc.
}