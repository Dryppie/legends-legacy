namespace Services.LL.Interfaces.Combat.Reward.Idle;

public interface IIdleDungeonSigilDropPool
{
    IReadOnlyList<string> GetAdditionalSigilIds(string areaId);
}
