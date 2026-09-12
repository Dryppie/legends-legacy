'use strict';
let teamPreview = null, teamPlanVersion = 0, teamPlanUrl = null, teamNames = new Map();
function teamBusy() { $('find-teams').disabled = active || !teamPreview; }
function invalidateTeamPlan() {
  ++teamPlanVersion; teamPreview = null; $('team-preview').hidden = true; teamBusy();
  if (teamPlanUrl) URL.revokeObjectURL(teamPlanUrl); teamPlanUrl = null;
  $('team-plan-status').textContent = 'Settings changed. Review the study before running.';
}
function teamFloorChanged() {
  const floor = Number($('team-floor').value);
  $('team-slots').value = String(floor < 5 ? 4 : floor < 10 ? 5 : floor === 10 ? 6 : 7);
  teamReferenceAvailability();
}
function teamReferenceAvailability() {
  const compatible = $('team-floor').value === '1' && $('team-slots').value === '4';
  $('team-user-reference').disabled = !compatible;
  if (!compatible) $('team-user-reference').checked = false;
}
async function readTeamFile(id, fallback) {
  const file = $(id).files[0]; if (!file) return fallback;
  if (file.size > 32 * 1024 * 1024) throw new Error('Study imports must be no larger than 32 MiB.');
  return JSON.parse(await file.text());
}
function teamTable(headers, rows) {
  const wrap = node('div', null, 'table-wrap'), table = node('table'), head = node('thead'), heading = node('tr'), body = node('tbody');
  for (const title of headers) heading.append(node('th', title)); head.append(heading); table.append(head, body); wrap.append(table);
  for (const values of rows) { const tr = node('tr'); for (const value of values) tr.append(node('td', value)); body.append(tr); }
  return wrap;
}
function teamBuilds(party, title) {
  const disclosure = node('details'); disclosure.append(node('summary', title));
  for (const member of party) {
    const build = member.build;
    disclosure.append(node('p', `Character ${member.partySlot}: ${build.essenceIds.length ? build.essenceIds.map(id => teamNames.get(id) || id).join(' / ') : 'Essences generated independently'}`));
    disclosure.append(node('p', `Equipment: ${build.equipment.map(e => e.definitionId.replace('plain.', '').replace('.rarity.uncommon', '').replaceAll('_', ' ')).join(', ')}`, 'hint'));
  }
  const exact = node('details'); exact.append(node('summary', 'Exact ordered builds and identity'), node('pre', JSON.stringify(party, null, 2))); disclosure.append(exact);
  return disclosure;
}
function displayTeamPreview(plan) {
  teamPreview = plan; const d = plan.definition, b = d.budget, improvement = d.mode === 'improve-supplied';
  $('team-preview').hidden = false;
  $('team-budget').textContent = `Floor ${b.priorityFloor} · ${d.requiredPartySize} characters · ${improvement ? 'Improve saved builds' : 'Independent discovery'} · ${b.essenceSlots} Essences each · level ${b.characterLevel} · Uncommon ${b.quality}, tier ${b.tier}, rank ${b.rank} · ${d.contexts.length} equipment context(s) · ${d.budgetPurpose}. ${d.allowedEssences.length} allowed Essences; ${d.ownedCopies ? 'declared owned copies' : 'hypothetical ownership'}.`;
  $('team-cost').replaceChildren(teamTable(['Stage', 'Reserved combats'], Object.entries(plan.cost).map(([key, count]) => [key.replace(/([A-Z])/g, ' $1'), count.toLocaleString()])));
  $('team-preview-builds').replaceChildren(...d.contexts.map(c => teamBuilds(c.characterTemplates, `Equipment context: ${c.id} · all ${c.characterTemplates.length} characters`)));
  $('team-preview-references').replaceChildren(node('h3', `${d.references.length} benchmark reference(s)`));
  const parents = new Set((d.starts || []).map(start => start.referenceId));
  if (improvement) $('team-preview-references').append(node('p', `${parents.size} supplied search parent(s). These teams are measured inside each arm's discovery budget.`, 'hint'));
  for (const r of d.references) {
    const panel = node('div', null, 'boss-strategy'); panel.append(node('strong', r.id), node('p', `${r.context} · ${r.source}`, 'hint'), teamBuilds(r.scenario.party, 'Inspect reference party'));
    if (parents.has(r.id)) panel.prepend(node('p', 'Used as a supplied search parent in this study.', 'hint'));
    $('team-preview-references').append(panel);
  }
  if (!d.references.length) $('team-preview-references').append(node('p', 'No benchmark references registered. Generated teams can still establish scoped viability.', 'hint'));
  $('team-seed-sources').textContent = `${d.excludedCombatSeeds.length.toLocaleString()} excluded combat seeds. ${plan.seedSources.join(' · ')}. ${plan.note}`;
  $('team-plan').textContent = JSON.stringify(d, null, 2);
  if (teamPlanUrl) URL.revokeObjectURL(teamPlanUrl);
  teamPlanUrl = URL.createObjectURL(new Blob([JSON.stringify(d, null, 2)], { type: 'application/json' }));
  $('team-plan-download').href = teamPlanUrl; $('team-plan-download').download = `${d.id}.json`;
  $('team-plan-status').textContent = `Ready: up to ${plan.cost.total.toLocaleString()} combats. The displayed definition is the one that will run. Diagnostics and unused replay reserves are not automatically spent.`;
  teamBusy();
}
async function previewTeams(imported = false) {
  invalidateTeamPlan(); const version = teamPlanVersion;
  $('team-plan-status').textContent = 'Checking budgets, references and recorded seed history…';
  try {
    let plan;
    if (imported) {
      const definition = await readTeamFile('team-definition-file', null);
      if (!definition) throw new Error('Select a complete study definition first.');
      plan = await api('/api/team-plan/import', definition);
    } else {
      if (!$('team-form').reportValidity()) return;
      let ownedCopies = null;
      if ($('team-ownership').value === 'owned') {
        ownedCopies = {};
        for (const line of $('team-copies').value.split('\n').map(s => s.trim()).filter(Boolean)) {
          const match = /^([^=\s]+)\s*=\s*(\d+)$/.exec(line);
          if (!match || Object.hasOwn(ownedCopies, match[1])) throw new Error('Enter each owned Essence once as Essence ID = count.');
          ownedCopies[match[1]] = Number(match[2]);
        }
      }
      const excludedSeeds = await readTeamFile('team-exclusions', []), references = await readTeamFile('team-references', []);
      if (!Array.isArray(excludedSeeds) || !Array.isArray(references)) throw new Error('Seed and reference imports must each be JSON arrays.');
      plan = await api('/api/team-plan', { floor: Number($('team-floor').value), slots: Number($('team-slots').value), seed: Number($('team-seed').value),
        purpose: $('team-purpose').value, includeUserReference: $('team-user-reference').checked,
        allowedEssences: $('team-restrict-pool').checked ? [...$('team-pool').selectedOptions].map(o => o.value) : null,
        ownedCopies, excludedSeeds, references });
    }
    if (version !== teamPlanVersion) return; displayTeamPreview(plan);
  } catch (e) { if (version === teamPlanVersion) $('team-plan-status').textContent = `Study unavailable: ${e.message}`; }
}
function renderTeamResults(result) {
  $('team-results').hidden = !result.isStudy; $('rows').closest('.table-wrap').hidden = !!result.isStudy;
  if (!result.isStudy) return;
  $('metrics').after($('team-results'));
  const study = result.study, verified = result.integrity === 'Reconstructed', conclusion = study.conclusion, improvement = result.studyDefinition.mode === 'improve-supplied';
  $('team-assessments').replaceChildren();
  for (const [title, value] of [['Execution', study.status], [improvement ? 'Retained-search viability' : 'Generated viability', conclusion?.generatedViability || 'Unavailable'],
    ['Frozen family', study.balance?.assessment || 'Unavailable'], ['Overall balance', verified ? conclusion?.overallAssessment || 'Unavailable' : 'Unverified'], ['Evidence', result.integrity]]) {
    const card = node('div', null, 'team-status'); card.dataset.outcome = value; card.append(node('span', title), node('strong', value)); $('team-assessments').append(card);
  }
  $('team-results-note').textContent = `Floor ${result.studyDefinition.budget.priorityFloor} · ${result.studyDefinition.budgetPurpose}. ${improvement ? 'Reference-derived improvement: supplied teams were scored as search parents; inherited ancestry is retained. ' : 'Independent discovery: no supplied search parents. '}${result.isPartialEvidence ? 'Partial or historical evidence: no verified recommendation. ' : ''}The strongest generated primary stays frozen; confirmation never selects a new winner. References alone do not establish generated viability. Draws are non-wins. Family size ${study.balance?.familySize || 0}. No acceptance is claimed for unsearched parties or other floors.`;
  $('team-accounting').replaceChildren(teamTable(['Stage', 'Attempted', 'Completed'], Object.keys(study.accounting.attempted).map(stage => [stage, study.accounting.attempted[stage], study.accounting.completed[stage]])),
    node('p', `Reserved ${study.accounting.reserved}; hard cap ${study.accounting.maximum}; unused ${study.accounting.unusedReservation}.`, 'hint'));
  $('team-result-cards').replaceChildren();
  const view = $('team-results-view').value;
  for (const member of study.confirmation?.members || []) {
    if (view === 'generated' && !member.generatedIds.length || view === 'references' && !member.referenceIds.length) continue;
    const cell = study.balance?.cells.find(c => c.id === member.cellId), recipe = study.confirmation.definition.cells.find(c => c.id === member.cellId)?.scenario;
    const card = node('article', null, 'boss-strategy');
    const sources = [...member.generatedIds.map(id => `${member.primary ? 'Primary' : 'Generated alternative'} ${id.slice(0, 12)}`), ...member.referenceIds.map(id => `Reference: ${id}`)];
    card.append(node('h3', sources.join(' · ')), node('p', `${member.context} · ${cell?.wins ?? 0}/${cell?.valid ?? 0} wins · planned ${cell?.planned ?? 0} · ${cell?.draws ?? 0} draws`));
    card.append(node('p', `50% ceiling check: ${cell?.outcome || 'Unavailable'}. 10% viability: ${cell?.lowerSupported ? 'Supported' : 'Not established'}.`, 'hint'));
    const interval = rate => rate ? `${format(rate.lower * 100, '%')}–${format(rate.upper * 100, '%')}` : 'unavailable';
    card.append(node('p', `Pointwise 95%: ${interval(cell?.pointwiseInterval)}. Family-adjusted: ${interval(cell?.adjustedInterval)}. ${cell?.observedAboveCeiling ? 'ABOVE 50% CEILING.' : ''}`, 'hint'));
    if (member.generatedIds.length && member.referenceIds.length) card.append(node('p', improvement ? 'Exact retained recipe: shared confirmation samples, search and reference labels preserved. This is not independent rediscovery.' : 'Independent convergence: one exact recipe, shared confirmation samples, both provenances retained.', 'hint'));
    if (recipe) { card.append(teamBuilds(recipe.party, 'Full ordered team and fixed equipment')); const link = node('a', 'Export frozen party recipe ↓', 'download'); link.href = `/api/runs/${selectedRun}/team-recipe/${member.cellId}`; card.append(link); }
    $('team-result-cards').append(card);
  }
  if (!$('team-result-cards').children.length) $('team-result-cards').append(node('p', 'No frozen confirmation recipes in this view. Partial discovery does not produce a confirmed team.', 'hint'));
  $('team-comparisons').replaceChildren(node('h3', 'Paired reference comparisons'), teamTable(['Generated cell', 'Reference', 'Context', 'Gained / lost wins', 'Difference', 'Paired interval'], study.comparisons.map(p => [p.generatedCell.slice(0, 17), p.referenceId, p.context,
    `${p.gainedWins} / ${p.lostWins}`, format(p.difference.meanChange, ' pp'), `${format(p.difference.lower)}–${format(p.difference.upper)} pp`])), node('p', 'Descriptive paired differences; absolute acceptance thresholds still apply.', 'hint'));
  $('team-earlier').replaceChildren(...(conclusion?.notes || []).map(note => node('p', note)), teamTable(['Stage', 'Party', 'Context', 'Wins / samples', 'Confirmed'], (conclusion?.earlierBreaches || []).map(b => [b.stage, b.partyId.slice(0, 12), b.context, `${b.wins}/${b.samples}`, b.confirmed ? 'Yes' : 'Unresolved'])),
    teamTable(['Selection recipe', 'Worst-context wins', 'Guardian health', 'Survival'], study.selection.map(r => [r.id.slice(0, 12), format(r.fitness.worstContextWinRate * 100, '%'), format(r.fitness.guardianHealth, '%'), format(r.fitness.survival, '%')])));
}
async function initStudies() {
  $('team-floor').replaceChildren(...session.floors.map(f => new Option(`Floor ${f.floorNumber} · ${f.guardianName} · ${f.requiredSlots} characters`, f.floorNumber)));
  $('team-slots').replaceChildren(...session.bossBudgets.map(b => new Option(`${b.essenceSlots} slots · level ${b.characterLevel} · ${b.quality}, T${b.tier} R${b.rank}`, b.essenceSlots)));
  $('team-floor').value = '1'; teamFloorChanged(); $('team-seed').value = crypto.getRandomValues(new Int32Array(1))[0];
  try {
    const options = await api('/api/team-options'); teamNames = new Map(options.pool.map(e => [e.id, e.name]));
    $('team-pool').replaceChildren(...options.pool.map(e => new Option(`${e.name} · ${e.id}`, e.id)));
    $('team-assumptions').textContent += ' ' + options.note;
  } catch (e) { $('team-plan-status').textContent = `Pool preview unavailable: ${e.message}`; }
}
$('team-mode').addEventListener('change', () => { const legacy = $('team-mode').value === 'historical'; $('boss-search').hidden = !legacy; $('team-search').hidden = legacy; if (legacy && !bossReady) updateBossPlan(); });
$('team-form').addEventListener('input', invalidateTeamPlan);
$('team-form').addEventListener('change', invalidateTeamPlan);
$('team-definition-file').addEventListener('change', invalidateTeamPlan);
$('team-floor').addEventListener('change', teamFloorChanged);
$('team-slots').addEventListener('change', teamReferenceAvailability);
$('team-restrict-pool').addEventListener('change', () => { $('team-pool').disabled = !$('team-restrict-pool').checked; });
$('team-ownership').addEventListener('change', () => { $('team-copies').disabled = $('team-ownership').value !== 'owned'; });
$('team-new-seed').addEventListener('click', () => { $('team-seed').value = crypto.getRandomValues(new Int32Array(1))[0]; invalidateTeamPlan(); });
$('team-form').addEventListener('submit', event => { event.preventDefault(); previewTeams(); });
$('team-import').addEventListener('click', () => previewTeams(true));
$('team-results-view').addEventListener('change', () => { if (details?.isStudy) renderTeamResults(details); });
$('find-teams').addEventListener('click', async () => {
  if (!teamPreview) return;
  const hash = teamPreview.planHash; invalidateTeamPlan(); error('');
  try { paintJob(await api('/api/teams', { planHash: hash })); $('team-plan-status').textContent = 'Study started. Follow Run progress below.'; }
  catch (e) { error(e.message); }
});
