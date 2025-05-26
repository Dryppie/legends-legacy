using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Professions;

namespace Application.UseCases.Professions.Dtos;
public class ProfessionDto : IMapFrom<Profession>
{
    public ProfessionType ProfessionType { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int ExperienceUntilNextLevel { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Profession, ProfessionDto>();
    }
}