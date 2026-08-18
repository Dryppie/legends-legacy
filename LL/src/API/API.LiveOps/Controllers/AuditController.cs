using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using API.LiveOps.Authorization;
using Application.UseCases.Administration;
using Application.UseCases.Administration.Commands.RecordAuditExport;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Queries.GetAdministrationAudit;
using Common.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.LiveOps.Controllers;

[Route("api/liveops/audit")]
public sealed class AuditController : LiveOpsControllerBase
{
    public sealed record AuditExportRequest(
        Guid OperationId,
        DateTimeOffset From,
        DateTimeOffset To,
        string? Source,
        string? ActionType,
        string? Actor,
        string? Permission,
        string? Reference,
        string? RiskLevel,
        string? Target,
        Guid? TargetOperationId);

    [HttpGet]
    [Authorize(Policy = AdministrationPermissions.Read)]
    public async Task<ActionResult<Response<AdministrationAuditPageDto>>> Get(
        [FromQuery] string? cursor,
        [FromQuery] int take = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string? source = null,
        [FromQuery] string? actionType = null,
        [FromQuery] string? actor = null,
        [FromQuery] string? permission = null,
        [FromQuery] string? reference = null,
        [FromQuery] string? riskLevel = null,
        [FromQuery] string? target = null,
        [FromQuery] Guid? operationId = null)
    {
        var result = await Mediator.Send(new GetAdministrationAuditQuery(
            cursor,
            take,
            from,
            to,
            source,
            actionType,
            actor,
            permission,
            reference,
            riskLevel,
            target,
            operationId,
            LiveOpsAuthorization.HasPermission(User, AdministrationPermissions.SuperAdmin)));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("exports")]
    [Authorize(Policy = AdministrationPermissions.SuperAdmin)]
    [EnableRateLimiting("audit-exports")]
    public async Task<IActionResult> Export([FromBody] AuditExportRequest request)
    {
        if (request.OperationId == Guid.Empty)
        {
            return BadRequest(Response<Guid>.Fail("A non-empty export operation ID is required."));
        }
        if (request.From > request.To)
        {
            return BadRequest(Response<Guid>.Fail(
                "The audit export start date must not be after the end date."));
        }
        if (request.To - request.From > TimeSpan.FromDays(31))
        {
            return BadRequest(Response<Guid>.Fail(
                "Audit exports must cover 31 days or less."));
        }

        const int maximumRows = 5_000;
        var entries = new List<AdministrationAuditEntryDto>();
        string? cursor = null;
        do
        {
            var page = await Mediator.Send(new GetAdministrationAuditQuery(
                cursor,
                100,
                request.From,
                request.To,
                request.Source,
                request.ActionType,
                request.Actor,
                request.Permission,
                request.Reference,
                request.RiskLevel,
                request.Target,
                request.TargetOperationId,
                true));
            if (!page.IsSuccess || page.Data is null)
            {
                return BadRequest(page);
            }

            entries.AddRange(page.Data.Entries);
            cursor = page.Data.NextCursor;
            if (entries.Count >= maximumRows && cursor is not null)
            {
                return BadRequest(Response<Guid>.Fail(
                    "The export exceeds 5,000 rows. Narrow the date range or filters."));
            }
        } while (cursor is not null);

        var csv = BuildCsv(entries);
        var detailsJson = JsonSerializer.Serialize(new
        {
            request.From,
            request.To,
            request.Source,
            request.ActionType,
            request.Actor,
            request.Permission,
            request.Reference,
            request.RiskLevel,
            request.Target,
            request.TargetOperationId,
            RowCount = entries.Count,
            Sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(csv)))
        });
        var recorded = await Mediator.Send(new RecordAuditExportCommand(
            request.OperationId,
            CurrentActor,
            entries.Count,
            detailsJson));
        if (!recorded.IsSuccess)
        {
            return BadRequest(recorded);
        }

        var fileName = $"liveops-audit-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(new UTF8Encoding(true).GetBytes(csv), "text/csv; charset=utf-8", fileName);
    }

    private static string BuildCsv(IReadOnlyList<AdministrationAuditEntryDto> entries)
    {
        var csv = new StringBuilder();
        csv.AppendLine("OccurredAt,OperationId,Outcome,RiskLevel,Source,ActionType,Permission,ActorSubject,ActorDisplayName,TargetAccountId,TargetCharacterId,TargetResourceId,Reason,InternalNotes,DetailsJson");
        foreach (var entry in entries)
        {
            AppendRow(csv,
                entry.OccurredAt.ToString("O"),
                entry.OperationId.ToString(),
                entry.Outcome,
                entry.RiskLevel,
                entry.Source,
                entry.ActionType,
                entry.Permission,
                entry.ActorSubject,
                entry.ActorDisplayName,
                entry.TargetAccountId?.ToString(),
                entry.TargetCharacterId?.ToString(),
                entry.TargetResourceId?.ToString(),
                entry.Reason,
                entry.InternalNotes,
                entry.DetailsJson);
        }

        return csv.ToString();
    }

    private static void AppendRow(StringBuilder csv, params string?[] fields) =>
        csv.AppendLine(string.Join(',', fields.Select(CsvField)));

    private static string CsvField(string? value)
    {
        var safe = value ?? string.Empty;
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@')
        {
            safe = "'" + safe;
        }
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }
}
