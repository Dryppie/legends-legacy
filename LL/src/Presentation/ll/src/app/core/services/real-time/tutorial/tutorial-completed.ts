export interface TutorialCompletedMsg {
  tutorialId: string;
  rewardCinders?: number;
  nextRoute?: string | null;
  wasSkipped?: boolean;
}
