using System.Security.Claims;
using Application.Interfaces.Outbox;
using Common.Primitives;
using Domain.Models.Administration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace API.LiveOps.Operations;

public sealed class OperationTrackingFilter(IOperatorOperationRepository operations, AdministrationOperationContext correlation,
    IHostEnvironment environment, ILogger<OperationTrackingFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var metadata = (context.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo.GetCustomAttribute<TrackOperationAttribute>();
        if (metadata is null || !context.ModelState.IsValid) { await next(); return; }
        var actor = context.HttpContext.User.FindFirstValue("sub") ?? context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.HttpContext.User.FindFirstValue("oid");
        var request = context.ActionArguments.Values.FirstOrDefault(x => x?.GetType().GetProperty("OperationId")?.PropertyType == typeof(Guid));
        var id = request?.GetType().GetProperty("OperationId")?.GetValue(request) as Guid?;
        var target = context.ActionArguments.TryGetValue(metadata.Target, out var argument) ? argument as Guid?
            : request?.GetType().GetProperty(metadata.Target, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(request) as Guid?;
        if (id is null || id == Guid.Empty || target is null || target == Guid.Empty || string.IsNullOrWhiteSpace(actor)) {
            context.Result = new BadRequestObjectResult(Response<bool>.Fail("A valid operation reference and target are required.")); return;
        }
        try {
            if (!await operations.BeginAsync(new OperatorOperation { OperationId = id.Value, ActorSubject = actor,
                Environment = environment.EnvironmentName, Kind = metadata.Kind, Source = metadata.Source, TargetId = target.Value, TargetKind = metadata.TargetKind }, context.HttpContext.RequestAborted)) {
                context.Result = new ConflictObjectResult(Response<bool>.Fail("An unresolved action or a different registered request conflicts with this reference. Check Operations & recovery and keep the original operation reference.")); return;
            }
        } catch (Exception error) {
            logger.LogError(error, "Could not register operation {OperationId}; execution was not started.", id);
            context.Result = new ObjectResult(Response<bool>.Fail("Operation recovery storage is unavailable. The action was not started. Keep this operation reference and retry after recovery.")) { StatusCode = 503 }; return;
        }
        var previous = correlation.OperationId; correlation.OperationId = id;
        try {
            var executed = await next();
            // The durable initial state remains Unknown if the request/response is interrupted.
            if (executed.Exception is null && executed.Result is ObjectResult result) {
                var success = result.Value?.GetType().GetProperty("IsSuccess")?.GetValue(result.Value) as bool?;
                var outcome = success == true ? "Committed" : success == false && result.StatusCode is >= 400 and < 500 ? "Rejected" : "Unknown";
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try { await operations.FinishAsync(actor, environment.EnvironmentName, id.Value, outcome, deadline.Token); }
                catch (Exception error) { logger.LogWarning(error, "Operation {OperationId} needs receipt reconciliation.", id); }
            }
        } finally { correlation.OperationId = previous; }
    }
}
