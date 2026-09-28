using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Models.Administration;
using MediatR;
namespace Application.UseCases.Administration.Commands.SaveCompensationPackage;
public sealed record SaveCompensationPackageCommand(CompensationPackageEdit Edit, AdministrationActor Actor) : ICommand<Response<CompensationPackageDto>>;
public sealed class SaveCompensationPackageCommandHandler(ICompensationPackageService service, IMapper mapper) : IRequestHandler<SaveCompensationPackageCommand, Response<CompensationPackageDto>>
{
    public async Task<Response<CompensationPackageDto>> Handle(SaveCompensationPackageCommand request, CancellationToken ct)
    {
        var result = await service.SaveAsync(request.Edit, request.Actor, ct);
        return result.IsSuccess ? Response<CompensationPackageDto>.Success(mapper.Map<CompensationPackageDto>(result.Data)) :
            result.IsConflict ? Response<CompensationPackageDto>.Conflict(result.ErrorMessage, result.ErrorCode) : Response<CompensationPackageDto>.Fail(result.ErrorMessage);
    }
}
