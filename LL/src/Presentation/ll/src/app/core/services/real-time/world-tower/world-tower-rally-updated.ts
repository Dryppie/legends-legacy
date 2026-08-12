export interface WorldTowerRallyUpdated {
  rallyId: string;
  floorNumber: number;
  event: string;
  status: string;
  participantCount: number;
  requiredSlots: number;
  pendingApplicationCount: number;
  occurredAtUtc: string;
}
