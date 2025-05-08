import { Essence } from './essence';

export interface EssenceSlot {
  slotType: SlotType;
  slotState: SlotState;
  occupiedEssence?: Essence;
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
