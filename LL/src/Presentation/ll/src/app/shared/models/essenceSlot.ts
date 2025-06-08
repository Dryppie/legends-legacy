import { Essence } from './essence';

export interface EssenceSlot {
  id: string;
  slotType: SlotType;
  slotState: SlotState;
  occupiedEssence?: Essence | null;
}

export enum SlotType {
  Standard = 'Standard',
  Subscription = 'Subscription',
}

export enum SlotState {
  Active = 'Active',
  Reserved = 'Reserved',
  Locked = 'Locked',
}
