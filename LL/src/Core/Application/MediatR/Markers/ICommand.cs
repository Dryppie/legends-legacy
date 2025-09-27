using MediatR;

namespace Application.MediatR.Markers;
public interface ICommand<TResponse> : IRequest<TResponse>, ICommandBase { }