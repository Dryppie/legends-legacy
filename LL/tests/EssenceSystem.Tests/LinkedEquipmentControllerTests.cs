using API.LL.Common;
using API.LL.Controllers.V1;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Equipments.Queries.GetLinkedEquipment;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace EssenceSystem.Tests;

public sealed class LinkedEquipmentControllerTests
{
    [Fact]
    public async Task Disconnect_cancels_pending_lookup_without_faulting_controller_task()
    {
        using var cancellation = new CancellationTokenSource();
        var pending = new TaskCompletionSource<EquipmentInstanceDto?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var equipmentId = Guid.NewGuid();
        var sender = new LinkedEquipmentSender((query, token) =>
        {
            Assert.Equal(equipmentId, query.EquipmentId);
            Assert.Equal(cancellation.Token, token);
            return pending.Task.WaitAsync(token);
        });
        using var services = new ServiceCollection().AddSingleton<ISender>(sender).BuildServiceProvider();
        var controller = CreateController(services, cancellation.Token);

        var request = controller.GetLinked(equipmentId, cancellation.Token);
        Assert.False(request.IsCompleted);
        cancellation.Cancel();
        var response = await request;

        Assert.Equal(ClientDisconnectMiddleware.ClientClosedRequestStatusCode,
            Assert.IsType<StatusCodeResult>(response.Result).StatusCode);
        Assert.True(request.IsCompletedSuccessfully);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Only_cancellation_of_an_aborted_request_is_handled(bool aborted, bool cancellation)
    {
        using var requestCancellation = new CancellationTokenSource();
        if (aborted)
            requestCancellation.Cancel();
        Exception failure = cancellation
            ? new OperationCanceledException("Database operation cancelled")
            : new InvalidOperationException("Database operation failed");
        var sender = new LinkedEquipmentSender((_, _) => Task.FromException<EquipmentInstanceDto?>(failure));
        using var services = new ServiceCollection().AddSingleton<ISender>(sender).BuildServiceProvider();
        var controller = CreateController(services, requestCancellation.Token);

        if (aborted && cancellation)
        {
            var response = await controller.GetLinked(Guid.NewGuid(), requestCancellation.Token);
            Assert.Equal(ClientDisconnectMiddleware.ClientClosedRequestStatusCode,
                Assert.IsType<StatusCodeResult>(response.Result).StatusCode);
        }
        else
        {
            var thrown = await Assert.ThrowsAnyAsync<Exception>(
                () => controller.GetLinked(Guid.NewGuid(), requestCancellation.Token));
            Assert.Same(failure, thrown);
        }
    }

    private static EquipmentController CreateController(IServiceProvider services, CancellationToken requestAborted) => new()
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = services,
                RequestAborted = requestAborted
            }
        }
    };

    private sealed class LinkedEquipmentSender(
        Func<GetLinkedEquipmentQuery, CancellationToken, Task<EquipmentInstanceDto?>> lookup) : ISender
    {
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            (TResponse)(object)(await lookup(Assert.IsType<GetLinkedEquipmentQuery>(request), cancellationToken))!;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
