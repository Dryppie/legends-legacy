using Domain.Models.Administration;

namespace Application.UseCases.Administration.Dtos;

public sealed record SupportCasePageDto(IReadOnlyList<SupportCaseDto> Cases, int Total, int Page, int PageSize);
