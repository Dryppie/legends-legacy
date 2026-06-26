using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Colosseum;

namespace Application.UseCases.Colosseum.Dtos;
public class ArenaTicketStatusDto : IMapFrom<ArenaTicketStatus>
{
    public int CurrentTickets { get; set; }
    public DateTimeOffset LastTicketUpdate { get; set; }
    public int MaxTickets { get; set; }
    public DateTimeOffset? NextTicketAt { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArenaTicketStatus, ArenaTicketStatusDto>()
            .ForMember(dto => dto.NextTicketAt, opt => opt.MapFrom(src =>
                src.CurrentTickets >= src.MaxTickets
                    ? (DateTimeOffset?)null
                    : src.LastTicketUpdate.AddHours(3)));
    }
}
