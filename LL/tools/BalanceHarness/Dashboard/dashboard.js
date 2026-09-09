'use strict';
const $ = id => document.getElementById(id);
const node = (tag, text, className) => { const el = document.createElement(tag); if (text != null) el.textContent = text; if (className) el.className = className; return el; };
let session, catalogs = [], runs = [], active = false, job, handledJob, selectedRun, details, events = [], eventPage = 0, searchReady = false;
function error(message) { $('error').textContent = message || ''; $('error').hidden = !message; }
async function api(path, body) {
  const response = await fetch(path, body === undefined ? {} : { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Tower-Session': session.token }, body: JSON.stringify(body) });
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
    for (const run of runs.filter(r => id !== 'reference' || r.status === 'Complete'))
      $(id).append(new Option(`${run.name} · ${run.status} · ${run.valid}/${run.planned}`, run.id));
    $(id).value = runs.some(r => r.id === previous) ? previous : '';
  }
}
function row(target, values) { const tr = node('tr'); for (const value of values) tr.append(node('td', value)); $(target).append(tr); }
async function showRun(id) {
  const previousBattle = id === selectedRun ? $('battle').value : null;
  $('replay-result').hidden = true;
  selectedRun = id; details = null; $('report').hidden = true;
  if (!id) { $('evidence-status').textContent = 'Select a run to verify its evidence and review results.'; return; }
  $('evidence-status').textContent = 'Verifying saved inputs, content and battle results…';
  try {
    const result = await api(`/api/runs/${id}`); if (id !== selectedRun) return; details = result;
    $('evidence-status').textContent = 'Evidence verified. ' + (result.replayCompatible ? 'This build can replay these fights.' : 'Replays require the original build and runtime. You can still review these measurements.');
    $('report-title').textContent = `${result.definition.id} · ${result.report.status}`;
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
    for (const [value, label] of [[result.report.validBattles, 'VALID TRIALS'], [result.report.cells.length, 'COMBINATIONS'], [result.masterSeed, 'MASTER SEED']]) {
      const metric = node('div', null, 'metric'); metric.append(node('strong', value), node('span', label)); $('metrics').append(metric);
    }
    $('rows').replaceChildren();
    for (const c of result.report.cells) {
      const rate = c.clearRate;
      row('rows', [`Floor ${c.floor} / ${c.party}`, c.status, `${c.wins} / ${c.valid}`, rate ? `${format(rate.rate * 100, '%')} [${format(rate.lower * 100)}–${format(rate.upper * 100)}]` : '—', format(c.survivalPercent.mean, '%'), format(c.durationSeconds.mean, ' s'), format(c.guardianHealthPercent.mean, '%')]);
    }
    $('comparison').hidden = !result.comparison && !result.hasSavedComparison; $('comparison-rows').replaceChildren();
    if (result.comparison) {
      for (const c of result.comparison.cells) row('comparison-rows', [c.cell, c.status, `${c.gameplayChanges} / ${c.pairs}`, delta(c.clearRateChange?.meanChange, ' pp'), delta(c.survivalChange?.meanChange, ' pp'), delta(c.durationChange?.meanChange, ' s'), delta(c.guardianHealthChange?.meanChange, ' pp')]);
      $('comparison-note').textContent = `${result.comparison.status}. ` + result.comparison.cells.filter(c => c.reason).map(c => `${c.cell}: ${c.reason}`).join(' ') + ' Paired mean intervals need at least 30 trials and nonzero variance; these point estimates are descriptive.';
    } else $('comparison-note').textContent = 'The saved reference is not available in this results folder, so this comparison could not be verified here.';
    $('battle').replaceChildren(...result.battles.map(b => new Option(`${b.id} · seed ${b.seed} · ${b.outcome} · ${format(b.durationSeconds, ' s')}`, b.id)));
    if (previousBattle && result.battles.some(b => b.id === previousBattle)) $('battle').value = previousBattle;
    $('replay').disabled = active || !result.replayCompatible || !result.battles.length;
    $('replay-hint').textContent = result.replayCompatible ? 'Re-execute a saved trial and verify preparation, combat and outcome.' : 'Start the dashboard with this run’s retained executable and matching runtime to replay it.';
    $('report').hidden = false;
  } catch (e) { if (id === selectedRun) { $('evidence-status').textContent = 'Evidence could not be verified.'; throw e; } }
}
function paintJob(next) {
  job = next; active = ['Running', 'Cancelling'].includes(next?.status);
  $('job-status').textContent = next?.status ?? 'Ready'; $('job-message').textContent = next?.message ?? 'Your next measurement starts here.';
  $('progress').max = next?.planned || 1; $('progress').value = next?.completed || 0;
  $('counts').textContent = next ? `${next.kind} · ${next.completed} / ${next.planned}` : 'No active operation';
  $('cancel').disabled = next?.status !== 'Running'; $('replay').disabled = active || !details?.replayCompatible || !details?.battles.length;
  $('search').disabled = active || !searchReady;
  updateBudget();
}
async function poll() {
  try {
    const next = await api('/api/job'); paintJob(next);
    if (next && !active && handledJob !== next.id) {
      handledJob = next.id;
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
    $('catalog').replaceChildren(...catalogs.map(c => new Option(c.definition.id, c.id)));
    if (!catalogs.length) throw new Error('No valid Tower benchmark catalogs found in the configured catalog folder.');
    selectCatalog(); await refreshRuns(); poll();
    try {
      const plan = await api('/api/search-plan');
      $('search-budget').textContent = `${plan.candidates} candidates · ${plan.floors} floors · ${plan.discoverySamples} discovery trials per cell · ${plan.confirmationSamples} confirmation trials per shortlisted cell · up to ${plan.maximumBattles.toLocaleString()} battles`;
      searchReady = true; $('search').disabled = active;
    } catch (e) { $('search-budget').textContent = `Search unavailable: ${e.message}`; }
  } catch (e) { error(e.message); $('start').disabled = true; }
})();
