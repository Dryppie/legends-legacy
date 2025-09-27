using MediatR;

namespace Application.MediatR.Markers;
public interface IQuery<TResponse> : IRequest<TResponse> { }