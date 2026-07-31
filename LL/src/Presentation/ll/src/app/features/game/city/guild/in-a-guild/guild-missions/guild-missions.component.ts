import { Component } from '@angular/core';
import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import {
  GuildMissionOption,
  PersonalGuildOrder,
} from '../../../../../../shared/models/Dtos/guild/guildMission';
import { NumberFormatPipe } from '../../../../../../shared/pipes/number-format/number-format.pipe';
import { HumanizeEnumPipe } from '../../../../../../shared/pipes/enums/humanize-enum.pipe';
import { RegularButtonComponent } from '../../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-guild-missions',
  imports: [
    NgIf,
    NgFor,
    NgClass,
    DatePipe,
    NumberFormatPipe,
    HumanizeEnumPipe,
    RegularButtonComponent,
  ],
  templateUrl: './guild-missions.component.html',
  styleUrl: './guild-missions.component.scss',
})
export class GuildMissionsComponent {
  readonly missions;
  readonly loading;

  constructor(private readonly state: GuildStateService) {
    this.missions = this.state.missions;
    this.loading = this.state.loading;
  }

  selectMission(option: GuildMissionOption): void {
    if (!this.missions()?.canSelectMission || option.isSelected) return;
    this.state.selectMission(option.id);
  }

  claimOrder(order: PersonalGuildOrder): void {
    if (!order.canClaimReward) return;
    this.state.claimOrderReward(order.id);
  }

  claimWeeklyReward(): void {
    if (!this.missions()?.myWeeklyContribution?.canClaimReward) return;
    this.state.claimWeeklyMissionReward();
  }

  missionProgress(): number {
    const mission = this.missions()?.activeMission;
    if (!mission?.targetAmount) return 0;
    return Math.min(100, (mission.currentAmount / mission.targetAmount) * 100);
  }

  orderProgress(order: PersonalGuildOrder): number {
    if (!order.targetAmount) return 0;
    return Math.min(100, (order.currentAmount / order.targetAmount) * 100);
  }

  claimableOrderCount(orders: PersonalGuildOrder[]): number {
    return orders.filter((order) => order.canClaimReward).length;
  }

  weeklyRemaining(): number {
    const mission = this.missions()?.activeMission;
    if (!mission) return 0;
    return Math.max(0, mission.targetAmount - mission.currentAmount);
  }

  orderStatusLabel(order: PersonalGuildOrder): string {
    if (order.canClaimReward) return 'Complete';
    if (order.status === 'RewardClaimed') return 'Claimed';
    if (order.status === 'Expired') return 'Expired';
    return 'In progress';
  }

  tierClass(tier: string | undefined): string {
    switch (tier) {
      case 'Platinum':
        return 'll-badge-success';
      case 'Gold':
        return 'll-badge-accent';
      case 'Silver':
        return 'border-zinc-300/40 text-zinc-200';
      case 'Bronze':
        return 'border-amber-500/40 text-amber-300';
      default:
        return 'll-badge-muted';
    }
  }
}
