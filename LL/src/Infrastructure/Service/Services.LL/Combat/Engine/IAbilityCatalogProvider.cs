namespace Services.LL.Combat.Engine;

public interface IAbilityCatalogProvider
{
    AbilityCatalog GetCatalog();
}

public interface ICompiledAbilityCatalogProvider : IAbilityCatalogProvider
{
    CompiledAbilityCatalog GetCompiledCatalog();
}
