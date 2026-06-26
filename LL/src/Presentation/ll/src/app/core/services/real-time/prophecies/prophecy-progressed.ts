export interface ProphecyProgressedMsg {
  characterId: string;
  prophecyId: string;
  title: string;
  scope: string;
  slotType: string;
  status: string;
  currentValue: number;
  targetValue: number;
  amountGained: number;
  completed: boolean;
}
