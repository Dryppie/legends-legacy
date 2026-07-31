export type FirstPartyTourStepKind =
  | 'info'
  | 'click'
  | 'navigate'
  | 'waitForState';

export type FirstPartyTourSide = 'top' | 'right' | 'bottom' | 'left';
export type FirstPartyTourAlignment = 'start' | 'center' | 'end';

export interface FirstPartyTourAdvanceRule {
  event?: 'click' | 'pointerdown';
  selector?: string;
  route?: string;
  stateKey?: string;
}

export interface FirstPartyTourRestoreAction {
  type: 'click';
  selector: string;
}

export interface FirstPartyTourStepJson {
  id?: string;
  kind?: FirstPartyTourStepKind;
  element: string;
  includeSelectors?: string[];
  actionSelector?: string;
  title?: string;
  description: string;
  position?: FirstPartyTourSide;
  alignment?: FirstPartyTourAlignment;
  advanceOn?: FirstPartyTourAdvanceRule;
  route?: string;
  stateKey?: string;
  showBack?: boolean;
  showNext?: boolean;
  nextElement?: string;
  targetTimeoutMs?: number;
  waitForEnabled?: boolean;
  allowOutsideInteraction?: boolean;
  restoreOnBack?: FirstPartyTourRestoreAction;
}

export interface FirstPartyTourStep extends FirstPartyTourStepJson {
  id: string;
  kind: FirstPartyTourStepKind;
  position: FirstPartyTourSide;
  alignment: FirstPartyTourAlignment;
}

export interface FirstPartyTourStartOptions {
  force?: boolean;
}

export interface FirstPartyTourRect {
  top: number;
  right: number;
  bottom: number;
  left: number;
  width: number;
  height: number;
}

export interface FirstPartyTourViewState {
  pageId: string;
  step: FirstPartyTourStep;
  stepIndex: number;
  stepCount: number;
  targetRect: FirstPartyTourRect | null;
  canGoBack: boolean;
  canGoNext: boolean;
  canFinish: boolean;
  blocksInteraction: boolean;
  instruction: string | null;
}

export interface FirstPartyTourHistoryEntry {
  stepIndex: number;
  route: string;
  scrollX: number;
  scrollY: number;
}

export type FirstPartyTourStatePredicate = () => boolean;
