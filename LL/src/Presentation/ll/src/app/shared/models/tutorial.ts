export const FIRST_STEPS_TUTORIAL_ID = 'tutorial.first_steps';
export const TUTORIAL_STEP_DEFEAT_TRAINING_CREATURE =
  'defeat_training_creature';
export const TUTORIAL_STEP_ABSORB_ESSENCE = 'absorb_essence';
export const TUTORIAL_STEP_EQUIP_ESSENCE = 'equip_essence';
export const TUTORIAL_STEP_CRAFT_EQUIPMENT = 'craft_equipment';
export const TUTORIAL_STEP_EQUIP_EQUIPMENT = 'equip_equipment';
export const TUTORIAL_TRAINING_GROUNDS_AREA_ID =
  'tutorial_area_training_grounds';
export const TOUR_STATE_TUTORIAL_EQUIPMENT_COMPLETE =
  'tutorial.equipment.complete';
export const TOUR_STATE_TUTORIAL_CRAFTING_READY =
  'tutorial.crafting.ready';

export interface TutorialState {
  tutorialId: string;
  title: string;
  version: number;
  currentStep: string;
  objective: string;
  currentAmount: number;
  requiredAmount: number;
  presentation?: TutorialStepPresentation | null;
  actionLabel: string;
  destinationRoute: string;
  guidePageId?: string | null;
  tourPageId?: string | null;
  isCompleted: boolean;
}

export interface TutorialStepPresentation {
  actionLabel: string;
  destinationRoute: string;
  guidePageId?: string | null;
  tourPageId?: string | null;
}
