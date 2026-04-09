# DataverseServices

Lightweight Dataverse service layer for early-bound C# projects.

## Design

- `IService` for standard Dataverse access
- `IBulkService` for batch and bulk operations
- `ColumnSetFactory` for early-bound column selection
- one project, folder-based structure
- no metadata provider
- no runtime guards
- primary id convention: `{logicalname}id`

## Notes

- Package reference uses `Microsoft.CrmSdk.CoreAssemblies` for compatibility with .NET Framework 4.7.2 consumers via `netstandard2.0`.
- `ExecuteMultiple` remains useful for mixed batches.
- `CreateMultiple`, `UpdateMultiple`, and `UpsertMultiple` are included for same-table bulk operations.
- `UpdateMultiple` and `UpsertMultiple` are not supported by every table, so explicit fallback methods are included.

## Structure

```text
src/
  DataverseServices/
    Abstractions/
      IService.cs
      IBulkService.cs
    Infrastructure/
      ColumnSetFactory.cs
      Service.cs
      BulkService.cs
    Extensions/
      ServiceCollectionExtensions.cs
    DataverseServices.csproj
```

## Registration

```csharp
services.AddDataverseServices();
```

## Typical usage

```csharp
var account = service.GetByAlternateKey<Account>(
    new KeyAttributeCollection
    {
        { Account.Fields.AccountNumber, "A-10001" }
    },
    x => x.AccountId,
    x => x.Name,
    x => x.AccountNumber);
```

```csharp
bulkService.UpsertMultipleWithFallbackChunked(accounts, batchSize: 500);
```


## Target framework

- Library target: `netstandard2.0`
- Compatible with .NET Framework 4.7.2 consumers and modern .NET projects.
