using API.LL.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class DatabaseConflictExceptionHandlerTests
{
    [Fact]
    public async Task Duplicate_essence_loadout_name_returns_conflict_problem_details()
    {
        var handler = new DatabaseConflictExceptionHandler(
            NullLogger<DatabaseConflictExceptionHandler>.Instance);
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "request-loadout-409"
        };
        context.Request.Path = "/api/v1/essence/loadouts/loadout-id";
        context.Response.Body = new MemoryStream();
        var postgresException = new PostgresException(
            "duplicate key value violates unique constraint",
            "ERROR",
            "ERROR",
            PostgresErrorCodes.UniqueViolation,
            detail: null,
            hint: null,
            position: 0,
            internalPosition: 0,
            internalQuery: null,
            where: null,
            schemaName: "public",
            tableName: "EssenceLoadouts",
            columnName: null,
            dataTypeName: null,
            constraintName: "IX_EssenceLoadouts_CharacterId_Name",
            file: "nbtinsert.c",
            line: "664",
            routine: "_bt_check_unique");

        var handled = await handler.TryHandleAsync(
            context,
            new DbUpdateException("Save failed.", postgresException),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(
            "An Essence loadout with that name already exists.",
            body.RootElement.GetProperty("detail").GetString());
        Assert.Equal(
            "essence_loadout_name_conflict",
            body.RootElement.GetProperty("code").GetString());
        Assert.Equal("conflict", body.RootElement.GetProperty("category").GetString());
        Assert.Equal("request-loadout-409", body.RootElement.GetProperty("requestId").GetString());
    }

    [Fact]
    public async Task Unrelated_database_error_is_not_handled()
    {
        var handler = new DatabaseConflictExceptionHandler(
            NullLogger<DatabaseConflictExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            new DefaultHttpContext(),
            new DbUpdateException("Save failed."),
            CancellationToken.None);

        Assert.False(handled);
    }
}
