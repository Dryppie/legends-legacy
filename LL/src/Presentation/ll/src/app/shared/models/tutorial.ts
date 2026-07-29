export const FIRST_STEPS_TUTORIAL_ID = 'tutorial.first_steps';
export const TUTORIAL_STEP_DEFEAT_TRAINING_CREATURE =
  'defeat_training_creature';
export const TUTORIAL_STEP_ABSORB_ESSENCE = 'absorb_essence';
export const TUTORIAL_STEP_EQUIP_ESSENCE = 'equip_essence';
export const TUTORIAL_STEP_EQUIP_EQUIPMENT = 'equip_equipment';
export const TUTORIAL_STEP_START_LUMO_RUINS = 'start_lumo_ruins';
export const TUTORIAL_TRAINING_GROUNDS_AREA_ID =
  'tutorial_area_training_grounds';
export const TUTORIAL_LUMO_RUINS_AREA_ID = 'region_01_area_01';
export const TUTORIAL_GOBLIN_ESSENCE_DEFINITION_ID = 'essence.legacy.goblin';

export interface TutorialState {
  tutorialId: string;
  title: string;
  version: number;
  currentStep: string;
  objective: string;
  currentAmount: number;
  requiredAmount: number;
  currentStepIndex: number;
  totalSteps: number;
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

export interface TutorialCompletion {
  tutorialId: string;
  rewardCinders: number;
  nextRoute: string;
  wasSkipped: boolean;
}
