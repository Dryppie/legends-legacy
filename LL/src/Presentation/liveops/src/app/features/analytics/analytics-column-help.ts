import { AdoptionColumn } from './adoption-preferences';

export interface AnalyticsColumnHelp { label: string; description: string; }
const column = (label: string, description: string): AnalyticsColumnHelp => ({ label, description });

// Keep these explanations aligned with TelemetryRepository and ItemizationCohortReport/ChoiceSummary.
export const activityHelp = {
  dau: 'Daily active accounts: distinct accounts that opened or refocused the visible game on this UTC day. Multiple visits count once; this is not a character or concurrent-player count.',
  wau: 'Weekly active accounts: distinct accounts active at least once in the seven UTC days ending on this report day. Each account counts once across the window. Do not sum daily WAU values.',
  mau: 'Monthly active accounts: distinct accounts active at least once in the 30 UTC days ending on this report day. This is a rolling window, not a calendar month. Do not sum daily MAU values.',
  newActive: 'Accounts created on this UTC day that were also active on the same day. Registrations without recorded activity are excluded.',
  returningActive: 'Active accounts other than those created on this UTC day: DAU minus New active. They do not need to have been inactive previously; this does not specifically measure reactivated players.',
  d1: 'Of all accounts created exactly one UTC day before this report day, the share active on the report day. Counts show returned / created accounts. An em dash means there were no accounts in that creation cohort.',
  d7: 'Of all accounts created exactly seven UTC days before this report day, the share active on the report day. This measures return on day seven, not any return within a week. Counts show returned / created accounts; an em dash means an empty creation cohort.',
};
export const activityColumns = [
  column('UTC day', 'The calendar day covered by the report, from midnight UTC to the next midnight UTC. It is not the report generation date or your local calendar day.'),
  column('DAU', activityHelp.dau), column('WAU', activityHelp.wau), column('MAU', activityHelp.mau),
  column('New active', activityHelp.newActive), column('D1', activityHelp.d1), column('D7', activityHelp.d7),
];

export const contentColumns = [
  column('Content', 'The game mode being measured: dungeon, Colosseum, tower, raid or region boss. Each mode records starts and results differently.'),
  column('Key', 'The content grouping: dungeon or boss definition, tower floor, or the initiating Colosseum character’s pre-match rating band. A rating band spans 500 points; for example, rating 1000+ means 1000–1499.'),
  column('Started', 'Recorded starts on this UTC day. Group content counts participating character entries, so one run can contribute several starts. Results can occur on a later day; this is not a completion-rate denominator.'),
  column('Completed', 'Results recorded on this UTC day: completed dungeons, successful tower attempts, raids not repelled, and region-boss runs with at least one level defeated. Colosseum counts all played matches here, including losses and draws. Group content counts participant entries, not unique runs.'),
  column('Failed', 'Results recorded on this UTC day: failed or retreated dungeons, failed tower attempts, repelled raids, region-boss runs with no level defeated, and Colosseum losses for the initiating character. Colosseum losses also count as Completed. Starts and results need not occur on the same day.'),
  column('Unique characters', 'Distinct characters with a start or result recorded for this content key on the report day. Each counts once in this row, even with repeated attempts. Characters can appear in other rows; summing rows does not give a unique-player total.'),
];

export const adoptionHelp: Record<AdoptionColumn, string> = {
  name: 'The essence or combat style definition being measured. Copies of the same essence are grouped together; this is not an individual owned item.',
  cohort: 'The account-activity window: 7 or 30 UTC days ending on the report day. Characters belong to accounts active at least once in this window. Ownership and selections are measured at snapshot time, not summed over the window.',
  level: 'The character’s level band at snapshot time, not their level when they were active. Comparisons match the same band, but its membership can change between snapshots.',
  rate: 'Observed characters divided by cohort characters, shown as a percentage and a count fraction. Each character counts once for this definition. This is not combat usage and is not adjusted for unlock requirements. An em dash means no usable denominator.',
  observed: 'Distinct cohort characters matching the selected measure at snapshot time: owning the essence, having it in a usable saved loadout, or having the combat style selected. Multiple copies or saved presets count once per character. It does not count battles.',
  population: 'All characters in this level band belonging to accounts active in the selected window, including characters without the definition. This is the adoption denominator; account activity does not prove each character was played. Fewer than 10 is marked as a small sample.',
  gap: 'Essence ownership percentage minus saved-loadout percentage for the same definition, activity window and level band, with equal denominators. For example, 80% owned and 30% saved gives a 50 percentage-point gap. An em dash means a matching pair is unavailable.',
  change: 'Current adoption percentage minus adoption in the selected comparison report, in percentage points (pp). A rise from 20% to 30% is +10 pp. The definition, measure, window and level band must match; cohort membership can change. Missing comparisons show an em dash, not zero.',
};

const winInterval = 'A conservative 95% interval around the mean of each character’s individual win rate. Each character has equal weight, regardless of battle count; draws count as non-wins. At least 30 characters with battle observations are required. This describes observed outcomes, not a causal benefit from the build.';
export const itemizationColumns = [
  column('Context / rules', 'The source or action that produced these observations, followed by the attribute rules version used. Compare matching contexts and versions; equipment awards, decisions and battles have different denominators.'),
  column('Slot / tier', 'The item’s equipment type and tier. For a whole-build observation, the slot is “build” and the tier is the highest equipped item tier (zero if none). This is not character level or an average equipment tier.'),
  column('Doctrine / essences', 'The combat style and number of equipped essences captured with the build. “None” and zero can also mean no build snapshot was attached to an equipment event; they do not always mean a player chose no style or essences.'),
  column('Characters', 'Distinct characters with any observation in this cohort on the report day, including equipment events. This can exceed the number of characters with battles used for the win interval. A character may appear in several cohorts.'),
  column('Awarded / equipped / dismantled', 'Counts of recorded item award, equip and dismantle events on this UTC day. Repeated actions can count more than once. These are separate event counts, not a funnel tracking the same items from award to disposal.'),
  column('Battles / wins / draws', 'Recorded character battle observations, followed by how many were wins and draws. One character can contribute several battles. These totals are not unique encounters or the equal-weight character average used for the interval.'),
  column('Character-average win interval (95%)', winInterval),
];

function distributionColumns(attribute: string, observations: string): AnalyticsColumnHelp[] {
  return [column('Attribute', attribute), column('Observations', observations),
    ...[10, 50, 90, 99].map(p => column('P' + p,
      `The ${p === 50 ? 'median: at least half' : p + 'th percentile: at least ' + p + '%'} of the recorded values for this attribute are at or below this amount. Uses the value at rank rounded up from ${p}% of the observation count; P50 uses the lower middle value for an even count. This is a distribution value, not a win rate or confidence bound.`)),
  ];
}
export const itemizationDistributionColumns = {
  spend: distributionColumns('The equipment stat whose budget spend is measured, adjusted for stat cost and divided by the item’s tier scale. Values are budget units, not raw stat points or percentages.',
    'Recorded item-stat values in this cohort. The same item or character can contribute through multiple equipment events; this is not a count of unique items or characters.'),
  waste: distributionColumns('The attribute whose purchased equipment budget was unused because the effective stat reached a cap. Values are estimated unused budget units for supported percentage stats, not raw excess points or a percentage of all gear.',
    'Attribute values from recorded build snapshots, including repeated snapshots and zero unused budget. Repeated events or battles can contribute again; this is not a count of independent characters.'),
  effective: distributionColumns('The effective attribute value captured for the build after applicable rules and caps. Units follow the attribute, such as points or percentage points; values are not all on a common scale.',
    'One value for this attribute per distinct character/build snapshot in the cohort. Repeated identical builds for the same character count once; a character who changes builds can contribute more than once.'),
};

export const itemizationCombinationColumns = [
  column('Attributes', 'A pair or triple of stats with positive values on equipment in the same build. The combination can span multiple equipped items; it need not occur on one item.'),
  column('Builds', 'Distinct character/build snapshots containing this combination. Repeated identical builds for a character count once. A single build can contain many pairs and triples, so do not sum rows for total builds.'),
  column('Characters', 'Distinct characters with at least one build containing this combination in the cohort. Multiple builds for the same character count once in this row.'),
];
export const itemizationEssenceColumns = [
  column('Essence', 'The essence definition captured in the equipped build for recorded battles. This measures battle participation, unlike ownership or saved-loadout adoption.'),
  column('Ascension / slot', 'The essence’s ascension tier and its position in the captured loadout. Slot numbering starts at 0: slot 0 is the first equipped essence. Different tiers and positions appear in separate rows.'),
  column('Characters', 'Distinct characters observed battling with this essence at this ascension tier and slot. Each character counts once in this row, regardless of repeated battles.'),
  column('Battles / wins', 'Battle observations containing this essence at this tier and slot, followed by wins among those observations. One battle can appear in several essence rows. Wins do not establish that this essence caused the result.'),
];
export const itemizationOutcomeColumns = [
  column('Attribute band', 'The effective attribute value at battle time, grouped by a lower bound that is included and an upper bound that is excluded. For example, 20–40 includes 20 but not 40; ∞ means no upper limit.'),
  column('Characters / battles', 'Distinct characters and total battle observations in this attribute band. Repeated battles add to the second count; a character whose build changes can appear in several bands.'),
  column('Character mean win rate', 'First calculate wins divided by battles for each character in this band, then average those rates with equal weight per character. Draws count as non-wins. This can differ from total wins divided by total battles.'),
  column('95% interval', winInterval),
];
export const itemizationChoiceColumns = [
  column('Attribute', 'The equipment stat present with a positive value on the candidate item or an eligible compatible replacement. Only complete decisions with an eligible candidate contribute to these columns.'),
  column('Available decisions', 'Eligible, complete decisions where the candidate or at least one eligible compatible replacement offered a positive value for this stat. Each decision counts once, regardless of how many items offered it.'),
  column('Selected decisions', 'Eligible, complete decisions where the candidate had a positive value for this stat. “Selected” means equipped for equip events, but only inspected for comparison events; it does not always mean the item was used.'),
  column('Selected with stat-free alternative', 'Selected decisions where at least one eligible compatible replacement had zero of this particular stat. “Stat-free” does not mean no stats at all. Other stats, quality and tier can differ, so this is not proof of preference for the stat.'),
];
