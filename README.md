# EFCoreAddQueryFilter
EF Core only supports `.HasQueryFilter` which overwrites the previous filter per entity.
Furthermore, it only supports query filters assigned to a concrete entity type.

This package provides a set of extensions that allows adding multiple query filters.

## Usages

Add a query filter on a specific entity
```csharp
public void Configure(EntityTypeBuilder<MyEntity> builder)
{
    modelBuilder.AddQueryFilter(x => x.IsVisible);
}
```

Add a query filter on multiple entities.
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddQueryFilterOnAllEntities<ISoftDeletable>(x => !x.IsDeleted);
}
```
