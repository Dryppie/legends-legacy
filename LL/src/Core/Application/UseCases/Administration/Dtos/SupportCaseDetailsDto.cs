using Domain.Models.Administration;

namespace Application.UseCases.Administration.Dtos;

public sealed record SupportCaseDetailsDto(SupportCaseDto Case, IReadOnlyList<SupportCaseEntryDto> Entries, int? NextBeforeSequence);
