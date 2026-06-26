using Application.Common.Mappings;
using AutoMapper;
using Domain.Models.Colosseum;

namespace Application.UseCases.Colosseum.Dtos;
public class ArenaOpponentPreviewDto : IMapFrom<ArenaOpponentPreview>
{
    // ----------------- opponent identity -----------------
    public Guid OpponentId { get; set; }
    public Guid CharacterId { get; set; }
    public string Name { get; set; } = null!;
    public int Level { get; set; }
    public int OpponentRating { get; set; }
    public string RankTier { get; set; } = string.Empty;
    public string RankTierId { get; set; } = string.Empty;

    // ----------------- caller’s potential outcome -----------------
    public int DeltaIfVictory { get; set; }
    public int DeltaIfDefeat { get; set; }
    public int DeltaIfDraw { get; set; }
    public int GloryIfVictory { get; set; }
    public int GloryIfDraw { get; set; }
    public int GloryIfDefeat { get; set; }

    // Optional convenience (often handy in the UI)
    //public int CurrentPlayerRating { get; set; }

    public void Mapping(Profile profile)
    {
            profile.CreateMap<ArenaOpponentPreview, ArenaOpponentPreviewDto>()
            // source = ArenaOpponentPreview.Opponent  ────────────────────────────────
            .ForMember(dto => dto.OpponentId, opt => opt.MapFrom(src => src.Opponent.Id))
            .ForMember(dto => dto.CharacterId, opt => opt.MapFrom(src => src.Opponent.Id))
            .ForMember(dto => dto.Name, opt => opt.MapFrom(src => src.Opponent.Name))
            .ForMember(dto => dto.Level, opt => opt.MapFrom(src => src.Opponent.Level))
            .ForMember(dto => dto.OpponentRating, opt => opt.MapFrom(src => src.Opponent.ArenaProfile.Rating))
            .ForMember(dto => dto.RankTier, opt => opt.MapFrom(src => ArenaRank.GetTier(src.Opponent.ArenaProfile.Rating).Name))
            .ForMember(dto => dto.RankTierId, opt => opt.MapFrom(src => ArenaRank.GetTier(src.Opponent.ArenaProfile.Rating).Id))
            // source = ArenaOpponentPreview.RatingDelta  ─────────────────────────────
            .ForMember(dto => dto.DeltaIfVictory, opt => opt.MapFrom(src => src.RatingDelta.DeltaIfVictory))
            .ForMember(dto => dto.DeltaIfDefeat, opt => opt.MapFrom(src => src.RatingDelta.DeltaIfDefeat))
            .ForMember(dto => dto.DeltaIfDraw, opt => opt.MapFrom(src => src.RatingDelta.DeltaIfDraw))
            .ForMember(dto => dto.GloryIfVictory, opt => opt.MapFrom(_ => 12))
            .ForMember(dto => dto.GloryIfDraw, opt => opt.MapFrom(_ => 8))
            .ForMember(dto => dto.GloryIfDefeat, opt => opt.MapFrom(_ => 5));
            //.ForMember(dto => dto.CurrentPlayerRating, opt => opt.MapFrom(src => src.RatingDelta.CurrentRating));
    }
}
