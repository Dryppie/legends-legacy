export interface RaidDirectoryUpdated {
  raidRunId: string;
  raidBossId: string;
  event: string;
  status: string;
  signupCount: number;
  version: number;
  occurredAtUtc: string;
}
