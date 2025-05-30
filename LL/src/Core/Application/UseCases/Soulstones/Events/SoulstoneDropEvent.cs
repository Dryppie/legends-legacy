using MediatR;

namespace Application.UseCases.Soulstones.Events;
public record SoulstoneDropEvent(Guid CharacterId, int SoulstonesEarned) : INotification;
