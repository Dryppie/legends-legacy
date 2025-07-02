using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Colosseum;

namespace Application.UseCases.Colosseum.Dtos;
public class ArenaOpponentPreviewDto : IMapFrom<ArenaOpponentPreview>
{
    // ----------------- opponent identity -----------------
    public Guid CharacterId { get; set; }
    public string Name { get; set; } = null!;
    public int Level { get; set; }
    public int OpponentRating { get; set; }

    // ----------------- caller’s potential outcome -----------------
    public int DeltaIfVictory { get; set; }
    public int DeltaIfDefeat { get; set; }
    public int DeltaIfDraw { get; set; }

    // Optional convenience (often handy in the UI)
    //public int CurrentPlayerRating { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<ArenaOpponentPreview, ArenaOpponentPreviewDto>()
            // source = ArenaOpponentPreview.Opponent  ────────────────────────────────
            .ForMember(dto => dto.CharacterId, opt => opt.MapFrom(src => src.Opponent.Id))
            .ForMember(dto => dto.Name, opt => opt.MapFrom(src => src.Opponent.Name))
            .ForMember(dto => dto.Level, opt => opt.MapFrom(src => src.Opponent.Level))
            .ForMember(dto => dto.OpponentRating, opt => opt.MapFrom(src => src.Opponent.ArenaRating))
            // source = ArenaOpponentPreview.RatingDelta  ─────────────────────────────
            .ForMember(dto => dto.DeltaIfVictory, opt => opt.MapFrom(src => src.RatingDelta.DeltaIfVictory))
            .ForMember(dto => dto.DeltaIfDefeat, opt => opt.MapFrom(src => src.RatingDelta.DeltaIfDefeat))
            .ForMember(dto => dto.DeltaIfDraw, opt => opt.MapFrom(src => src.RatingDelta.DeltaIfDraw));
            //.ForMember(dto => dto.CurrentPlayerRating, opt => opt.MapFrom(src => src.RatingDelta.CurrentRating));
    }
}