export interface EssenceAbilityData {
  kind: 'magnitude' | 'keyword';
  title: string;
  description?: string;
  detail?: string;
  base?: number;
  attr?: string | null;
  scale?: number;
  scaleDisplay?: string;
  bonus?: number;
  total?: string;
  attrValue?: number;
  unit?: string;
  resultLabel?: string;
  hasRange?: boolean;
  rollDisplay?: string;
  note?: string;
}
