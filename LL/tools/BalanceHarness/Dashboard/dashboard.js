'use strict';
const $ = id => document.getElementById(id);
const node = (tag, text, className) => { const el = document.createElement(tag); if (text != null) el.textContent = text; if (className) el.className = className; return el; };
let session, catalogs = [], runs = [], active = false, job, handledJob, selectedRun, details, events = [], eventPage = 0, searchReady = false;
let loadoutReady = false, loadoutPlanVersion = 0;
let bossReady = false, bossPlanVersion = 0;
let bossLibraryKey = null, bossLibraryVersion = 0, bossReferenceVersion = 0;
let bossValidationKey = null, bossValidationVersion = 0;
let runRequestController = null, runRequestVersion = 0;
const bossRequest = () => ({ floor: Number($('boss-floor').value), slots: Number($('boss-slots').value),
  intent: $('boss-intent').value, effort: $('boss-effort').value, seed: Number($('boss-seed').value) });
function freshBossSeed() { $('boss-seed').value = crypto.getRandomValues(new Int32Array(1))[0]; }
function bossReferenceNote(d) {
  const r = d.refinement;
  return `${d.controls.length} retained references · up to ${d.finalists} frozen finalists. `
    + (r ? `${r.referenceSetId ? `Historical set ${r.referenceSetId}` : 'Declared reference set'} · anchor ${r.anchorId.slice(0, 12)}. ${r.referenceEvidenceStatus || 'The anchor guides search; fresh trials measure its outcomes. Mechanic diagnostics require a supported replacement plan.'}`
      : 'Historical search schema; retained controls are measured on the same confirmation schedule.');
}
async function updateBossPlan() {
  const version = ++bossPlanVersion; bossReady = false; $('find-boss-loadouts').disabled = true;
  bossLibraryKey = null; ++bossLibraryVersion; ++bossReferenceVersion;
  $('boss-reference-library').hidden = true; $('boss-reference-library').open = false;
  $('boss-library-selection').hidden = true; $('boss-reference-details').replaceChildren();
  bossValidationKey = null; ++bossValidationVersion;
  $('boss-validation-library').hidden = true; $('boss-validation-library').open = false;
  $('boss-validation-details').replaceChildren();
  $('boss-plan').textContent = ''; $('boss-reference').textContent = ''; $('boss-budget').textContent = 'Preparing the declared boss search budget…';
  try {
    if (!$('boss-seed').checkValidity() || !$('boss-seed').value) throw new Error('Enter a valid experiment seed.');
    const plan = await api('/api/boss-plan', bossRequest()); if (version !== bossPlanVersion) return;
    const d = plan.definition, b = d.budget;
    $('boss-budget').textContent = `Level ${b.characterLevel} · Uncommon ${b.quality} · tier ${b.tier}, rank ${b.rank} · ${b.essenceSlots} Essences per character · ${d.mutablePartySlots.length} searched party members · ${d.methods.length} methods × ${d.generationSeeds.length} generation seeds · ${d.discoverySamples} target-boss discovery seeds · ${d.confirmationSamples} fresh confirmation seeds on every floor · up to ${plan.maximumBattles.toLocaleString()} actual battles, including controls and reserved diagnostics.`;
    $('boss-reference').textContent = bossReferenceNote(d);
    if (d.refinement?.referenceSetId) {
      bossLibraryKey = `${d.budget.priorityFloor}/${d.budget.essenceSlots}`;
      $('boss-reference-library').hidden = false;
      $('boss-library-status').textContent = 'Open to inspect historical results, all-floor weaknesses and exact party recipes. No fights are run.';
    }
    if (plan.validation?.available) {
      bossValidationKey = `${d.budget.priorityFloor}/${d.budget.essenceSlots}`;
      $('boss-validation-library').hidden = false;
      $('boss-validation-status').textContent = `${plan.validation.evidenceStatus} These 100 paired target seeds are separate from the earlier 20- and 40-sample studies. New boss plans exclude the recorded seeds automatically.`;
      $('boss-validation-choice').replaceChildren(...plan.validation.recipes.map(r => new Option(
        `${r.label} · ${r.wins}/${r.samples} target clears`, r.id)));
    }
    $('boss-plan').textContent = JSON.stringify(d, null, 2);
    bossReady = true; $('find-boss-loadouts').disabled = active;
  } catch (e) { if (version === bossPlanVersion) $('boss-budget').textContent = `Boss search unavailable: ${e.message}`; }
}
async function loadBossLibrary() {
  if (!$('boss-reference-library').open || !bossLibraryKey || !$('boss-library-selection').hidden) return;
  const key = bossLibraryKey, version = ++bossLibraryVersion;
  $('boss-library-status').textContent = 'Loading retained historical recipes…';
  try {
    const library = await api(`/api/boss-references/${key}`);
    if (version !== bossLibraryVersion || key !== bossLibraryKey) return;
    if (!library.available) throw new Error('No historical references are available for this budget.');
    $('boss-library-status').textContent = `${library.recipes.length} retained recipes · ${library.evidenceStatus} Samples from separate studies are never pooled. The discovery-selected anchor is kept even when another finalist later records more clears.`;
    $('boss-reference-choice').replaceChildren(...library.recipes.map(r => new Option(
      `${r.isAnchor ? 'Anchor · ' : ''}${r.id.slice(0, 12)} · historical ${r.wins}/${r.samples} target clears`, r.id)));
    $('boss-reference-choice').value = library.anchorId;
    $('boss-library-selection').hidden = false;
    await showBossReference();
  } catch (e) { if (version === bossLibraryVersion) $('boss-library-status').textContent = `Historical references unavailable: ${e.message}`; }
}
function historicalObservation(evidence, packageId, title) {
  const section = node('details'); section.open = title === 'Latest historical observation';
  section.append(node('summary', `${title} · ${packageId}`));
  section.append(node('p', 'Each row uses its recorded floor and required allies. Repeated trial IDs mark context aliases, not independent samples. Changing only the target recipe’s floor does not reproduce this transfer matrix.'));
  const wrap = node('div', null, 'table-wrap'), table = node('table'), head = node('thead'), heading = node('tr'), body = node('tbody');
  for (const text of ['Floor', 'Ally context', 'Clears / trials', 'Draws', 'Guardian health', 'Survival']) heading.append(node('th', text));
  head.append(heading); table.append(head, body); wrap.append(table);
  const observations = new Map();
  for (const cell of evidence.confirmation.cells) {
    const key = `${cell.floor}:${cell.trials.join(',')}`, alias = observations.get(key);
    if (!alias) observations.set(key, cell.context);
    const tr = node('tr');
    for (const value of [cell.floor, `${cell.context}${alias ? ` · same observations as ${alias}` : ''}`,
      `${cell.clears.filter(Boolean).length} / ${cell.clears.length}`, cell.draws,
      format(cell.guardianHealth, '%'), format(cell.survival, '%')]) tr.append(node('td', value));
    body.append(tr);
  }
  section.append(wrap);
  return section;
}
async function showBossReference() {
  const key = bossLibraryKey, id = $('boss-reference-choice').value, version = ++bossReferenceVersion;
  if (!key || !id) return;
  const target = $('boss-reference-details'); target.replaceChildren(node('p', 'Loading this recipe’s historical evidence…'));
  try {
    const result = await api(`/api/boss-references/${key}/${encodeURIComponent(id)}`);
    if (version !== bossReferenceVersion || key !== bossLibraryKey) return;
    const evidence = result.evidence, b = result.budget;
    target.replaceChildren(node('h3', `${id.slice(0, 12)}${id === result.anchorId ? ' · Discovery-selected anchor' : ' · Retained finalist'}`));
    target.append(node('p', `Level ${b.characterLevel} · Uncommon ${b.quality} · tier ${b.tier}, rank ${b.rank} · ${b.essenceSlots} Essences per character · ${evidence.targetRecipe.party.length} required target party members.`, 'hint'));
    target.append(node('p', 'Historical results describe their captured content, settings and execution. They do not guarantee current clears. Export preserves the original target seeds; repeating them adds no fresh evidence.', 'hint'));
    const exportLink = node('a', 'Export complete historical target recipe ↓', 'download');
    exportLink.href = `/api/boss-references/${key}/${encodeURIComponent(id)}/recipe`; target.append(exportLink);
    const recipe = node('details'); recipe.append(node('summary', 'Ordered Essences, fixed identities and exact equipment'));
    for (const member of evidence.targetRecipe.party) recipe.append(node('p', `Character ${member.partySlot}: ${member.build.essenceIds.join(' / ')}`));
    recipe.append(node('pre', JSON.stringify(evidence.targetRecipe, null, 2))); target.append(recipe);
    target.append(historicalObservation(evidence, result.provenance.packageId, 'Latest historical observation'));
    for (const previous of evidence.priorObservations || [])
      target.append(historicalObservation(previous.evidence, previous.packageId, 'Separate earlier observation'));
  } catch (e) { if (version === bossReferenceVersion) target.replaceChildren(node('p', `Historical recipe unavailable: ${e.message}`)); }
}
async function showBossValidation() {
  const key = bossValidationKey, id = $('boss-validation-choice').value, version = ++bossValidationVersion;
  if (!key || !id || !$('boss-validation-library').open) return;
  const target = $('boss-validation-details'); target.replaceChildren(node('p', 'Loading fixed validation evidence…'));
  try {
    const result = await api(`/api/boss-validations/${key}/${encodeURIComponent(id)}`);
    if (version !== bossValidationVersion || key !== bossValidationKey) return;
    const e = result.evidence, s = e.summary, b = result.budget;
    target.replaceChildren(node('h3', `${e.label} · ${s.wins}/${s.samples} target clears`));
    target.append(node('p', `${id.slice(0, 12)} · Floor ${result.floor} · ${b.essenceSlots} Essences per character · ${e.targetRecipe.party.length} required party members · level ${b.characterLevel} · ${b.quality}, tier ${b.tier}, rank ${b.rank}.`, 'hint'));
    target.append(node('p', 'Three complete recipes were fixed before testing. This target-only validation did not establish a reliable build or rerun other floors. Earlier transfer weaknesses still apply. The historical discovery anchor is unchanged.', 'hint'));
    target.append(node('p', `${s.defeats} defeats · ${s.draws} draws · guardian health remaining ${format(s.guardianHealth, '%')} · party survival ${format(s.survival, '%')}.`, 'hint'));
    target.append(node('p', `Clear rate · descriptive Wilson 95% interval: ${format(s.wilson95.lower * 100, '%')}–${format(s.wilson95.upper * 100, '%')}.`, 'hint'));
    for (const pair of result.comparisons)
      target.append(node('p', `Against ${pair.label}: ${pair.gained} gained wins, ${pair.lost} lost wins and ${pair.bothWin} shared winning seeds on the same ${pair.samples} paired trials.`, 'hint'));
    target.append(node('p', `Mean recovery: friendly reported healing ${format(s.friendlyHealing)} · friendly effective regeneration ${format(s.friendlyRegeneration)} · guardian reported healing ${format(s.guardianHealing)} · guardian effective regeneration ${format(s.guardianRegeneration)}. Reported healing and effective regeneration are distinct measures for initial participants, excluding summons; totals do not isolate causes.`, 'hint'));
    const exportLink = node('a', 'Export complete 100-seed validation recipe ↓', 'download');
    exportLink.href = `/api/boss-validations/${key}/${encodeURIComponent(id)}/recipe`; target.append(exportLink);
    target.append(node('p', 'Export preserves the original validation seeds. Repeating them reproduces recorded inputs and adds no fresh evidence.', 'hint'));
    const recipe = node('details'); recipe.append(node('summary', 'Ordered Essences, fixed identities and exact equipment'));
    for (const member of e.targetRecipe.party) recipe.append(node('p', `Character ${member.partySlot}: ${member.build.essenceIds.join(' / ')}`));
    recipe.append(node('pre', JSON.stringify(e.targetRecipe, null, 2))); target.append(recipe);
    const trials = node('details'); trials.append(node('summary', 'All 100 target observations'));
    const wrap = node('div', null, 'table-wrap'), table = node('table'), head = node('thead'), heading = node('tr'), body = node('tbody');
    for (const title of ['Seed', 'Outcome', 'Guardian health', 'Party survival']) heading.append(node('th', title));
    head.append(heading); table.append(head, body); wrap.append(table);
    for (const row of e.observations) {
      const tr = node('tr');
      for (const value of [row.seed, row.outcome, format(row.guardianHealth, '%'), format(row.survival, '%')]) tr.append(node('td', value));
      body.append(tr);
    }
    trials.append(wrap); target.append(trials);
  } catch (e) { if (version === bossValidationVersion) target.replaceChildren(node('p', `Fixed validation unavailable: ${e.message}`)); }
}
const loadoutRequest = () => ({ slots: Number($('loadout-slots').value), effort: $('loadout-effort').value, seed: Number($('loadout-seed').value), wholeParty: $('loadout-scope').value === 'whole' });
function freshLoadoutSeed() { $('loadout-seed').value = crypto.getRandomValues(new Int32Array(1))[0]; }
async function updateLoadoutPlan() {
  const version = ++loadoutPlanVersion; loadoutReady = false; $('find-loadouts').disabled = true;
  try {
    const plan = await api('/api/loadout-plan', loadoutRequest()); if (version !== loadoutPlanVersion) return;
    const b = plan.definition.budget;
    $('loadout-budget').textContent = `Level ${b.characterLevel} · Uncommon ${b.quality} · tier ${b.tier}, rank ${b.rank} · ${b.essenceSlots} Essences per character · priority floor ${b.priorityFloor} · all 15 floors · ${plan.definition.searchSeeds.length} search seeds · ${plan.definition.confirmationSamples} confirmation trials per floor/context · up to ${plan.maximumBattles.toLocaleString()} battles. Each cohort uses its own fixed budget.`;
    loadoutReady = true; $('find-loadouts').disabled = active;
  } catch (e) { if (version === loadoutPlanVersion) $('loadout-budget').textContent = `Search unavailable: ${e.message} Choose a new experiment seed if it was already used.`; }
}
function updateRecipeLink() {
  $('export-loadout').hidden = !details?.isLoadoutSearch || !$('battle').value;
  if (details?.isLoadoutSearch) $('export-loadout').href = `/api/runs/${selectedRun}/recipe/${encodeURIComponent($('battle').value)}`;
}
function error(message) { $('error').textContent = message || ''; $('error').hidden = !message; }
async function api(path, body, signal) {
  const response = await fetch(path, body === undefined ? { signal } : { signal, method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Tower-Session': session.token }, body: JSON.stringify(body) });
  const result = await response.json();
  if (!response.ok) throw new Error(result.error || `Request failed (${response.status}).`);
  return result;
}
const checked = id => [...$(id).querySelectorAll('input:checked')].map(input => input.value);
const catalog = () => catalogs.find(c => c.id === $('catalog').value);
const format = (value, suffix = '') => value == null ? '—' : Number(value).toFixed(2) + suffix;
const delta = (value, unit) => value == null ? '—' : (value > 0 ? '+' : '') + format(value, unit);
function choices(target, items, selected) {
  $(target).replaceChildren();
  for (const item of items) {
    const label = node('label', null, 'choice'), input = node('input');
    input.type = 'checkbox'; input.value = String(item.id); input.checked = !selected || selected.includes(item.id);
    input.addEventListener('change', updateSelection);
    label.append(input, node('span', item.label)); if (item.note) label.append(node('small', item.note));
    $(target).append(label);
  }
}
function selectCatalog(selection) {
  const c = catalog(); if (!c) return;
  choices('floors', c.definition.floors.map(id => {
    const floor = session.floors.find(f => f.floorNumber === id);
    return { id, label: `Floor ${id}`, note: floor ? `${floor.guardianName} · ${floor.requiredSlots} slots` : '' };
  }), selection?.floors);
  choices('parties', c.definition.parties.map(p => ({ id: p.id, label: p.id })), selection?.parties);
  $('samples').value = selection?.samples ?? c.definition.samplesPerCell;
  $('seed').value = selection?.seed ?? 1337;
  updateSelection();
}
function updateBudget() {
  const ids = checked('parties'), floors = checked('floors').length, count = Number($('samples').value), planned = ids.length * floors * count;
  $('budget').textContent = `${floors * ids.length} combinations · ${planned.toLocaleString()} battles`;
  $('start').disabled = active || !ids.length || !floors || count < 1 || count > 1000 || planned > 10000;
}
function updateSelection() {
  const c = catalog(); if (!c) return;
  const ids = checked('parties'); updateBudget();
  $('profiles').replaceChildren();
  for (const party of c.definition.parties.filter(p => ids.includes(p.id))) {
    const section = node('div', null, 'profile-party'); section.append(node('strong', `${party.id} party`));
    const groups = new Map();
    for (const floor of checked('floors')) {
      const cell = party.floorCellProfiles?.[floor] ?? party.cellProfiles, key = JSON.stringify(cell);
      if (!groups.has(key)) groups.set(key, { cell, floors: [] });
      groups.get(key).floors.push(floor);
    }
    if (!party.floorCellProfiles) {
      const chips = node('div', null, 'profile-chips');
      for (const profile of party.cellProfiles) chips.append(node('span', profile));
      section.append(chips);
    }
    for (const group of groups.values()) {
      const b = c.definition.profiles.find(p => p.id === group.cell[0]).build;
      const floors = party.floorCellProfiles ? `Floors ${group.floors.join(', ')} · ` : '';
      section.append(node('p', `${floors}Level ${b.characterLevel} · ${b.quality} tier ${b.tier} · rank ${b.rank} · ${b.essenceIds.length} Essences`, 'hint'));
    }
    const disclosure = node('details'); disclosure.append(node('summary', 'View equipment, Essences & assumptions'));
    disclosure.append(node('p', party.assumptions));
    for (const id of new Set([...groups.values()].flatMap(g => g.cell))) {
      const p = c.definition.profiles.find(p => p.id === id), b = p.build;
      disclosure.append(node('p', `${id} · level ${b.characterLevel} · tier ${b.tier} · rank ${b.rank}`));
      disclosure.append(node('p', 'Equipment: ' + b.equipment.map(e => e.definitionId.replace('plain.', '').replaceAll('_', ' ')).join(', ')));
      disclosure.append(node('p', 'Essences: ' + b.essenceIds.map(e => e.replace('essence.', '').replaceAll('_', ' ')).join(', ')));
      disclosure.append(node('p', p.assumptions));
    }
    section.append(disclosure); $('profiles').append(section);
  }
}
async function refreshRuns() {
  runs = await api('/api/runs');
  for (const [id, prompt] of [['saved-run', 'Select saved evidence'], ['reference', 'No comparison · new measurement']]) {
    const previous = $(id).value; $(id).replaceChildren(new Option(prompt, ''));
    for (const run of runs.filter(r => id !== 'reference' || (r.status === 'Complete' && r.kind === 'Benchmark')))
      $(id).append(new Option(`${run.kind} · ${run.name} · ${run.status} · ${run.valid}/${run.planned}`, run.id));
    $(id).value = runs.some(r => r.id === previous) ? previous : '';
  }
}
function row(target, values) { const tr = node('tr'); for (const value of values) tr.append(node('td', value)); $(target).append(tr); }
function renderBossAlternatives(result) {
  $('boss-alternatives').hidden = !result.isBossSearch; $('boss-strategies').replaceChildren(); $('boss-diagnostics').replaceChildren();
  if (!result.isBossSearch) return;
  const d = result.bossDefinition, report = result.bossSearch;
  const canonical = c => !report.contextAliases.some(a => a.floor === c.floor && a.context === c.context && a.evaluatedContext !== a.context);
  const targets = Object.keys(d.objective.targetFloorWeights).map(Number), controls = new Set(d.controls.map(p => p.id));
  $('boss-results-note').textContent = `${result.note} Target floor ${targets.join(', ')}. ${report.cacheHits} cache hits are separate from actual combats. Alternatives stay in their frozen discovery order; confirmation does not select new winners.`;
  $('boss-results-reference').textContent = bossReferenceNote(d);
  if (d.schemaVersion === 2 && result.diagnosticPlan?.status === 'unsupported')
    $('boss-results-note').textContent += ' No mechanic replacement starts were available: graph proposals use the joint-search fallback. Those arms are charged separately and do not establish independent graph-method evidence.';
  const view = $('boss-view').value, anchorId = d.refinement?.anchorId || d.controls[0]?.id;
  const visible = report.selection.filter(c => view === 'all' || (view === 'new' && !controls.has(c.id))
    || (view === 'controls' && controls.has(c.id)) || (view === 'anchor' && c.id === anchorId));
  for (const choice of visible) {
    const card = node('article', null, 'boss-strategy');
    card.append(node('strong', `${choice.id.slice(0, 12)} · ${choice.id === anchorId ? 'Reference anchor' : controls.has(choice.id) ? 'Retained reference' : 'Frozen alternative'}`), node('p', choice.source, 'hint'));
    const measured = report.confirmation.find(p => p.id === choice.id);
    const targetCells = measured?.cells.filter(c => targets.includes(c.floor) && canonical(c)) || [];
    if (!targetCells.length) card.append(node('p', 'Unconfirmed exploratory party. No recommendation is established.'));
    for (const cell of targetCells) {
      const wins = cell.clears.filter(Boolean).length;
      card.append(node('p', `Floor ${cell.floor} · ${cell.context}: ${wins}/${cell.clears.length} clears${wins ? '' : ' · unsuccessful on these trials'} · guardian health ${format(cell.guardianHealth, '%')} · survival ${format(cell.survival, '%')}`));
    }
    if (measured?.behavior.recovery) {
      const recovery = measured.behavior.recovery;
      card.append(node('p', `Mean target recovery · friendly reported healing ${format(measured.behavior.healing)} · friendly regeneration ${format(recovery.friendlyRegeneration)} · guardian healing ${format(recovery.guardianHealing)} · guardian regeneration ${format(recovery.guardianRegeneration)}.`, 'hint'));
    }
    const pairedDetails = node('details'); pairedDetails.append(node('summary', `Paired comparisons and transfer weaknesses · ${d.controls.length} retained references`));
    if (measured) for (const control of d.controls.filter(c => c.id !== choice.id)) {
      const reference = report.confirmation.find(p => p.id === control.id); if (!reference) continue;
      const comparisons = measured.cells.filter(canonical).map(cell => {
        const other = reference.cells.find(c => c.floor === cell.floor && c.context === cell.context);
        if (!other || other.clears.length !== cell.clears.length) return null;
        return { floor: cell.floor, context: cell.context,
          gained: cell.clears.filter((clear, i) => clear && !other.clears[i]).length,
          lost: cell.clears.filter((clear, i) => !clear && other.clears[i]).length };
      }).filter(Boolean);
      for (const paired of comparisons.filter(c => targets.includes(c.floor)))
        pairedDetails.append(node('p', `Paired vs ${control.id.slice(0, 12)} · ${paired.context}: ${paired.gained} gained / ${paired.lost} lost target clears.`, 'hint'));
      const weaker = comparisons.filter(c => !targets.includes(c.floor) && c.lost > c.gained);
      if (weaker.length) pairedDetails.append(node('p', `Observed transfer weaknesses vs ${control.id.slice(0, 12)}: ${weaker.map(c => `floor ${c.floor} (${c.context}; ${c.gained} gained / ${c.lost} lost)`).join('; ')}.`, 'hint'));
    }
    card.append(pairedDetails);
    const recipe = node('details'); recipe.append(node('summary', 'Ordered Essences and exact party dependencies'));
    recipe.append(node('p', `Fixed budget: level ${d.budget.characterLevel}, Uncommon ${d.budget.quality}, tier ${d.budget.tier}, rank ${d.budget.rank}. Later transfer participants outside the searched slots retain their authored builds; export the selected fight to include every required ally.`));
    for (const [slot, ids] of Object.entries(choice.builds))
      recipe.append(node('p', `Group ${Math.floor((Number(slot) - 1) / 5) + 1} · ${['Guardian', 'Restorer', 'Striker 1', 'Striker 2', 'Controller'][(Number(slot) - 1) % 5]}: ${ids.join(' / ')}`));
    if (targetCells[0]?.trials.length) {
      const link = node('a', 'Export target-floor party recipe ↓', 'download');
      link.href = `/api/runs/${selectedRun}/recipe/${encodeURIComponent(targetCells[0].trials[0])}`; recipe.append(link);
    }
    card.append(recipe);
    $('boss-strategies').append(card);
  }
  if (!report.selection.length) $('boss-strategies').append(node('p', 'This run has no complete confirmation package. Completed trials remain available for replay and export.'));
  else if (!visible.length) $('boss-strategies').append(node('p', 'No frozen recipes match this view. Choose retained references or all finalists to inspect the complete selection.'));
  const diagnostic = result.diagnosticPlan, diagnosticSection = $('boss-diagnostics');
  diagnosticSection.append(node('h3', d.schemaVersion === 2 ? 'Reserved A/B replacement diagnostic' : 'Reserved interaction diagnostic'));
  diagnosticSection.append(node('p', d.schemaVersion === 2
    ? 'The quartet measures the anchor, A alone, B alone and A+B on reserved paired seeds. A and B are whole-Essence substitutions; the stored enabler/consumer names do not establish synergy. Other effects, targeting, cooldowns and existing suppliers remain confounders.'
    : 'The saved quartet uses reserved paired seeds. Mechanic categories and reported healing do not establish causal benefits.', 'hint'));
  diagnosticSection.append(node('p', 'Recovery values are means over target encounters. Friendly reported direct/periodic/lifesteal healing retains observer attribution limits; effective regeneration is separate. Guardian totals refer to the original guardian. Totals may reflect fight duration and equipment/base regeneration; they do not establish deaths prevented. Missing values are unavailable.', 'hint'));
  if (diagnostic) {
    diagnosticSection.append(node('p', `Plan: ${diagnostic.status} · inserted A: ${diagnostic.enabler || 'unavailable'} · inserted B: ${diagnostic.consumer || 'unavailable'}.`));
    const note = node('details'); note.append(node('summary', 'Exact replacements, mechanism evidence and limitations'), node('p', diagnostic.note));
    diagnosticSection.append(note);
  } else diagnosticSection.append(node('p', 'The frozen diagnostic plan is unavailable in this archive. No replacement-specific interpretation is supplied.', 'hint'));
  if (report.diagnostics.length) {
    const table = node('table'), header = node('tr'), head = node('thead'), body = node('tbody');
    for (const label of ['Variant', 'Target clears', 'Guardian health', 'Friendly healing', 'Friendly regeneration', 'Guardian healing', 'Guardian regeneration']) header.append(node('th', label));
    head.append(header); table.append(head, body);
    const labels = d.schemaVersion === 2
      ? { baseline: 'Anchor', 'enabler-alone': 'A alone', 'consumer-alone': 'B alone', combination: 'A+B' }
      : { baseline: 'Reference', 'enabler-alone': 'Enabler alone', 'consumer-alone': 'Consumer alone', combination: 'Combination' };
    for (const measurement of report.diagnostics) {
      const source = diagnostic?.parties.find(p => p.id === measurement.id)?.source;
      const cells = measurement.cells.filter(canonical), recovery = measurement.behavior.recovery, tr = node('tr');
      const values = [labels[source] || measurement.id.slice(0, 12), cells.map(c => `F${c.floor}: ${c.clears.filter(Boolean).length}/${c.clears.length}`).join('; '),
        format(measurement.fitness.guardianHealth, '%'), format(measurement.behavior.healing), format(recovery?.friendlyRegeneration), format(recovery?.guardianHealing), format(recovery?.guardianRegeneration)];
      for (const value of values) tr.append(node('td', value)); body.append(tr);
    }
    const wrap = node('div', null, 'table-wrap'); wrap.append(table); diagnosticSection.append(wrap);
  } else diagnosticSection.append(node('p', 'No completed diagnostic measurements are available.', 'hint'));
  const audit = node('details'); audit.append(node('summary', 'Strategy evidence, context aliases and diagnostic limitations'));
  audit.append(node('p', 'Aliases identify authored contexts that became the same effective party. They are evaluated once and provide no extra independent observations. Mechanic categories describe measured behavior; unsupported attribution stays unknown.'));
  audit.append(node('pre', JSON.stringify({ strategyArchive: report.strategyArchive, contextAliases: report.contextAliases, diagnosticPlan: diagnostic, diagnostics: report.diagnostics }, null, 2)));
  $('boss-strategies').append(audit);
}
async function showRun(id) {
  const requestVersion = ++runRequestVersion;
  runRequestController?.abort(); runRequestController = null;
  $('results').setAttribute('aria-busy', 'false');
  const previousBattle = id === selectedRun ? $('battle').value : null;
  $('replay-result').hidden = true;
  selectedRun = id; details = null; $('report').hidden = true;
  $('loadout-builds').hidden = true; $('boss-alternatives').hidden = true; $('export-loadout').hidden = true;
  if (!id) { $('evidence-status').textContent = 'Select a run to verify its evidence and review results.'; return; }
  runRequestController = new AbortController();
  const signal = runRequestController.signal, started = performance.now();
  $('results').setAttribute('aria-busy', 'true');
  $('evidence-status').textContent = 'Verifying saved inputs, content and battle results…';
  const loadingTimer = setInterval(() => {
    if (requestVersion === runRequestVersion)
      $('evidence-status').textContent = `Verifying saved inputs, content and battle results… ${Math.floor((performance.now() - started) / 1000)} seconds elapsed. Selecting another run cancels this request.`;
  }, 1000);
  try {
    const result = await api(`/api/runs/${id}`, undefined, signal); if (requestVersion !== runRequestVersion) return; details = result;
    $('evidence-status').textContent = (result.isStudy ? `${result.integrity}. ${result.isPartialEvidence ? 'No verified recommendation. ' : ''}` : result.isPartialEvidence ? 'Incomplete experiment: saved file hashes verified; recommendations withheld. ' : 'Evidence verified. ')
      + (result.replayCompatible ? 'This build can replay these fights.' : 'Replays require the original build and runtime. You can still review these measurements.');
    $('report-title').textContent = `${result.definition.id} · ${result.report.status}`;
    renderBossAlternatives(result); renderTeamResults(result);
    $('download-md').href = `/api/runs/${id}/download/md`; $('download-json').href = `/api/runs/${id}/download/json`;
    let searchLinks = $('search-links');
    if (!searchLinks) { searchLinks = node('p'); searchLinks.id = 'search-links'; $('report-title').parentElement.after(searchLinks); }
    searchLinks.replaceChildren(); searchLinks.hidden = !result.hasSearchReport;
    if (result.hasSearchReport) {
      for (const [format, label] of [['md', 'Build search report'], ['json', 'Search evidence JSON']]) {
        const link = node('a', label, 'download'); link.href = `/api/runs/${id}/search/${format}`; link.target = '_blank'; link.rel = 'noopener';
        searchLinks.append(link, document.createTextNode(' '));
      }
    }
    $('metrics').replaceChildren();
    for (const [value, label] of [[result.report.validBattles, result.isStudy ? 'COMPLETED COMBATS' : 'VALID TRIALS'], [result.isStudy ? result.study.balance?.familySize || 0 : result.report.cells.length, result.isStudy ? 'FROZEN CELLS' : 'COMBINATIONS'], [result.masterSeed, result.isStudy ? 'GENERATION SEED' : result.isBossSearch ? 'DISCOVERY SEED' : 'MASTER SEED']]) {
      const metric = node('div', null, 'metric'); metric.append(node('strong', value), node('span', label)); $('metrics').append(metric);
    }
    $('rows').replaceChildren();
    for (const c of result.report.cells) {
      const rate = c.clearRate;
      row('rows', [result.isLoadoutSearch ? c.id : `Floor ${c.floor} / ${c.party}`, c.status, `${c.wins} / ${c.valid}`, rate ? `${format(rate.rate * 100, '%')} [${format(rate.lower * 100)}–${format(rate.upper * 100)}]` : '—', format(c.survivalPercent.mean, '%'), format(c.durationSeconds.mean, ' s'), format(c.guardianHealthPercent.mean, '%')]);
    }
    $('comparison').hidden = !result.comparison && !result.hasSavedComparison; $('comparison-rows').replaceChildren();
    if (result.comparison) {
      for (const c of result.comparison.cells) row('comparison-rows', [c.cell, c.status, `${c.gameplayChanges} / ${c.pairs}`, delta(c.clearRateChange?.meanChange, ' pp'), delta(c.survivalChange?.meanChange, ' pp'), delta(c.durationChange?.meanChange, ' s'), delta(c.guardianHealthChange?.meanChange, ' pp')]);
      $('comparison-note').textContent = `${result.comparison.status}. ` + result.comparison.cells.filter(c => c.reason).map(c => `${c.cell}: ${c.reason}`).join(' ') + ' Paired mean intervals need at least 30 trials and nonzero variance; these point estimates are descriptive.';
    } else $('comparison-note').textContent = 'The saved reference is not available in this results folder, so this comparison could not be verified here.';
    $('battle').replaceChildren(...result.battles.map(b => new Option(`${b.id} · seed ${b.seed} · ${b.outcome} · ${format(b.durationSeconds, ' s')}`, b.id)));
    if (previousBattle && result.battles.some(b => b.id === previousBattle)) $('battle').value = previousBattle;
    $('loadout-builds').hidden = !result.isLoadoutSearch || result.isBossSearch || !!result.isStudy; $('loadout-recipes').replaceChildren();
    if (result.isLoadoutSearch && !result.isBossSearch) {
      $('loadout-results-note').textContent = result.note;
      for (const party of result.loadouts) {
        const item = node('details'); item.append(node('summary', `${party.id.slice(0, 12)} · ${party.source}`));
        for (const [slot, ids] of Object.entries(party.builds)) item.append(node('p', `Group ${Math.floor((Number(slot) - 1) / 5) + 1} · ${['Guardian', 'Restorer', 'Striker 1', 'Striker 2', 'Controller'][(Number(slot) - 1) % 5]}: ${ids.map(id => id.replace('essence.', '').replaceAll('_', ' ')).join(' / ')}`));
        $('loadout-recipes').append(item);
      }
      if (result.deploymentComparisons) {
        const item = node('details'); item.append(node('summary', 'Compare deployments'));
        item.append(node('p', 'Repeated and alternating parties fill every required group. Their two authored context runs become identical after replacement; do not pool them as independent trials.'));
        for (const family of result.deploymentComparisons) for (const variant of family.variants)
          item.append(node('p', `${family.family} · ${variant.policy} · ${variant.id.slice(0, 12)} · ${variant.confirmed ? 'Confirmed on fresh seeds' : 'Discovery only'}`));
        $('loadout-recipes').append(item);
      }
    }
    updateRecipeLink();
    $('replay').disabled = active || !result.replayCompatible || !result.battles.length;
    $('replay-hint').textContent = result.replayCompatible ? 'Re-execute a saved trial and verify preparation, combat and outcome.' : 'Start the dashboard with this run’s retained executable and matching runtime to replay it.';
    $('report').hidden = false;
    error('');
  } catch (e) {
    if (requestVersion === runRequestVersion && e.name !== 'AbortError') {
      $('evidence-status').textContent = 'Evidence could not be verified.'; throw e;
    }
  } finally {
    clearInterval(loadingTimer);
    if (requestVersion === runRequestVersion) { runRequestController = null; $('results').setAttribute('aria-busy', 'false'); }
  }
}
function paintJob(next) {
  job = next; active = ['Running', 'Cancelling'].includes(next?.status);
  $('job-status').textContent = next?.status ?? 'Ready'; $('job-message').textContent = next?.message ?? 'Your next measurement starts here.';
  $('progress').max = next?.planned || 1; $('progress').value = next?.completed || 0;
  $('counts').textContent = next ? `${next.kind} · ${next.completed} / ${next.planned}` : 'No active operation';
  $('cancel').disabled = next?.status !== 'Running'; $('replay').disabled = active || !details?.replayCompatible || !details?.battles.length;
  $('search').disabled = active || !searchReady;
  $('find-loadouts').disabled = active || !loadoutReady;
  $('find-boss-loadouts').disabled = active || !bossReady;
  teamBusy();
  updateBudget();
}
async function poll() {
  try {
    const next = await api('/api/job'); paintJob(next);
    if (next && !active && handledJob !== next.id) {
      handledJob = next.id;
      if (next.kind === 'Loadouts') { freshLoadoutSeed(); updateLoadoutPlan(); }
      if (next.kind === 'Boss loadouts') { freshBossSeed(); updateBossPlan(); }
      if (next.status === 'Failed') error(next.message);
      await refreshRuns();
      if (next.run && runs.some(r => r.id === next.run)) { $('saved-run').value = next.run; await showRun(next.run); }
      if (next.kind === 'Replay' && next.status === 'Complete') {
        const replay = await api(`/api/jobs/${next.id}/replay`);
        events = replay.battle.eventLog || []; eventPage = 0; $('log-filter').value = '';
        $('replay-summary').textContent = `${replay.battle.scenarioId} · seed ${replay.battle.seed} · ${replay.battle.summary.contentOutcome} · ${format(replay.battle.summary.durationSeconds, ' s')} · guardian health ${format(replay.guardianHealthRemainingPercent, '%')} · ${events.length.toLocaleString()} events. Saved result matched.`;
        $('download-replay').href = `/api/jobs/${next.id}/replay`; $('replay-result').hidden = false; renderEvents();
      }
    }
  } catch (e) { error(e.message); }
  setTimeout(poll, 1000);
}
function renderEvents() {
  const search = $('log-filter').value.toLowerCase(), filtered = events.filter(e => `${e.eventType} ${e.details} ${e.statsSource}`.toLowerCase().includes(search));
  $('events').replaceChildren();
  for (const e of filtered.slice(eventPage * 100, (eventPage + 1) * 100)) row('events', [e.timestamp, e.eventType, e.details || e.source || '—']);
  $('event-count').textContent = `${filtered.length ? eventPage * 100 + 1 : 0}–${Math.min((eventPage + 1) * 100, filtered.length)} of ${filtered.length.toLocaleString()} matching events`;
  $('prev-events').disabled = eventPage === 0; $('next-events').disabled = (eventPage + 1) * 100 >= filtered.length;
}
$('catalog').addEventListener('change', () => { $('reference').value = ''; selectCatalog(); });
$('loadout-slots').addEventListener('change', updateLoadoutPlan);
$('loadout-scope').addEventListener('change', updateLoadoutPlan);
$('loadout-effort').addEventListener('change', updateLoadoutPlan);
$('loadout-seed').addEventListener('change', updateLoadoutPlan);
$('loadout-new-seed').addEventListener('click', () => { freshLoadoutSeed(); updateLoadoutPlan(); });
$('boss-view').addEventListener('change', () => { if (details?.isBossSearch) renderBossAlternatives(details); });
$('boss-reference-library').addEventListener('toggle', loadBossLibrary);
$('boss-reference-choice').addEventListener('change', showBossReference);
$('boss-validation-library').addEventListener('toggle', showBossValidation);
$('boss-validation-choice').addEventListener('change', showBossValidation);
for (const id of ['boss-floor', 'boss-slots', 'boss-intent', 'boss-effort', 'boss-seed']) $(id).addEventListener('change', updateBossPlan);
$('boss-new-seed').addEventListener('click', () => { freshBossSeed(); updateBossPlan(); });
$('find-boss-loadouts').addEventListener('click', async () => {
  error(''); $('find-boss-loadouts').disabled = true;
  try { paintJob(await api('/api/bosses', bossRequest())); }
  catch (e) { error(e.message); updateBossPlan(); }
});
$('battle').addEventListener('change', updateRecipeLink);
$('find-loadouts').addEventListener('click', async () => {
  error(''); $('find-loadouts').disabled = true;
  try { paintJob(await api('/api/loadouts', loadoutRequest())); }
  catch (e) { error(e.message); updateLoadoutPlan(); }
});
$('samples').addEventListener('input', updateSelection);
$('reference').addEventListener('change', async () => {
  try {
    error(''); if (!$('reference').value) return;
    const ref = await api(`/api/runs/${$('reference').value}`), match = catalogs.find(c => c.definition.id === ref.definition.id);
    if (!match) throw new Error('This reference uses a catalog that is not available here. Select a matching catalog before comparing.');
    $('catalog').value = match.id;
    selectCatalog({ floors: ref.definition.floors, parties: ref.definition.parties.map(p => p.id), samples: ref.definition.samplesPerCell, seed: ref.masterSeed });
    $('reference-hint').textContent = 'Loaded the reference’s floors, parties, sample count and seed. Recipes must also match for comparison.';
  } catch (e) { $('reference').value = ''; error(e.message); }
});
$('saved-run').addEventListener('change', () => { error(''); showRun($('saved-run').value).catch(e => error(e.message)); });
$('refresh').addEventListener('click', () => refreshRuns().catch(e => error(e.message)));
$('search').addEventListener('click', async () => {
  error(''); $('search').disabled = true;
  try { paintJob(await api('/api/search', {})); }
  catch (e) { error(e.message); $('search').disabled = !searchReady; }
});
$('run-form').addEventListener('submit', async event => {
  event.preventDefault(); error(''); $('start').disabled = true; $('replay-result').hidden = true;
  try { paintJob(await api('/api/runs', { catalog: $('catalog').value, floors: checked('floors').map(Number), parties: checked('parties'), samples: Number($('samples').value), seed: Number($('seed').value), reference: $('reference').value || null })); }
  catch (e) { error(e.message); updateSelection(); }
});
$('cancel').addEventListener('click', async () => { try { paintJob(await api(`/api/jobs/${job.id}/cancel`, {})); } catch (e) { error(e.message); } });
$('replay').addEventListener('click', async () => {
  error(''); $('replay').disabled = true; $('replay-result').hidden = true;
  try { paintJob(await api('/api/replays', { run: selectedRun, battle: $('battle').value })); }
  catch (e) { error(e.message); $('replay').disabled = !details?.replayCompatible; }
});
$('log-filter').addEventListener('input', () => { eventPage = 0; renderEvents(); });
$('prev-events').addEventListener('click', () => { eventPage--; renderEvents(); });
$('next-events').addEventListener('click', () => { eventPage++; renderEvents(); });
(async () => {
  try {
    session = await api('/api/session'); catalogs = session.catalogs;
    initStudies();
    $('boss-floor').replaceChildren(...session.floors.map(f => new Option(`Floor ${f.floorNumber} · ${f.guardianName}`, f.floorNumber)));
    $('boss-floor').value = '3';
    $('boss-slots').replaceChildren(...session.bossBudgets.map(b => new Option(`${b.essenceSlots} slots · level ${b.characterLevel} · ${b.quality}, T${b.tier} R${b.rank}`, b.essenceSlots)));
    $('boss-slots').value = '6'; $('metrics').after($('boss-alternatives'));
    freshBossSeed();
    $('catalog').replaceChildren(...catalogs.map(c => new Option(c.definition.id, c.id)));
    if (!catalogs.length) throw new Error('No valid Tower benchmark catalogs found in the configured catalog folder.');
    selectCatalog(); await refreshRuns(); poll();
    freshLoadoutSeed(); updateLoadoutPlan();
    try {
      const plan = await api('/api/search-plan');
      $('search-budget').textContent = `${plan.candidates} candidates · ${plan.floors} floors · ${plan.discoverySamples} discovery trials per cell · ${plan.confirmationSamples} confirmation trials per shortlisted cell · up to ${plan.maximumBattles.toLocaleString()} battles`;
      searchReady = true; $('search').disabled = active;
    } catch (e) { $('search-budget').textContent = `Search unavailable: ${e.message}`; }
  } catch (e) { error(e.message); $('start').disabled = true; }
})();
