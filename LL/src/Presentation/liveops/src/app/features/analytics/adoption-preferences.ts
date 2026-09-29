export type AdoptionColumn = 'name' | 'cohort' | 'level' | 'rate' | 'observed' | 'population' | 'gap' | 'change';
export type AdoptionSort = `${AdoptionColumn}-${'asc' | 'desc'}`;
export interface AdoptionPreferences {
  measure: string;
  sort: AdoptionSort;
  grouped: boolean;
  minimumCohort: number | null;
  comparisonDate: string;
  selectedReportDate: string;
}
export const createAdoptionPreferences = (): AdoptionPreferences => ({
  measure: 'essence-owned', sort: 'rate-desc', grouped: true, minimumCohort: null,
  comparisonDate: '', selectedReportDate: '',
});
export function compareLevelBands(a: string, b: string): number {
  return (Number.parseInt(a, 10) || 0) - (Number.parseInt(b, 10) || 0) || a.localeCompare(b);
}
