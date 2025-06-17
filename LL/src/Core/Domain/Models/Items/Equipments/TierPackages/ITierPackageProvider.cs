namespace Domain.Models.Items.Equipments.TierPackages;
public interface ITierPackageProvider
{
    TierPackage GetPackage(Rarity rarity);
}
