using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Colosseum;

namespace Application.UseCases.Colosseum.Dtos;
public class ArenaTicketStatusDto : IMapFrom<ArenaTicketStatus>
{
    public int CurrentTickets { get; set; }
    public DateTimeOffset LastTicketUpdate { get; set; }
    public int MaxTickets { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArenaTicketStatus, ArenaTicketStatusDto>();
    }
}