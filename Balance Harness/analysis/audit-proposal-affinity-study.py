"""Independent, zero-fight proposal study audit.

Recounts literal native battle reports, all five racing panels, pruning/selection,
the all-root barrier, held-out deduplication, reservation and planned endpoints.
The complementary native audit reconstructs RNG-driven proposals, affinity graph
derivation and production materialization; this script never imports the engine.
"""
import argparse
import copy
from contextvars import ContextVar
import datetime as dt
import gzip
import hashlib
import itertools
import json
import math
from pathlib import Path
import re
import statistics
import struct
import sys

VERSION = 'tower-proposal-affinity-comparison-v1'
CREATION_VERSION = 'tower-affinity-creation-comparison-v1'
SELECTOR_VERSION = 'tower-benchmark-tie-comparison-v1'
VALIDATION_VERSION = 'tower-benchmark-validation-comparison-v1'
PRESERVATION_VERSION = 'tower-affinity-preservation-comparison-v1'
ALLIED_VERSION = 'tower-affinity-allied-action-comparison-v1'
PLACEMENT_VERSION = 'tower-loadout-placement-comparison-v1'
NOMINATION_VERSION = 'tower-affinity-nomination-comparison-v1'
NOMINATION_RACING = 'tower-proposal-racing-v9'
PLACEMENT_RACING = 'tower-proposal-racing-v8'
PLACEMENT_POLICY = 'tower-proposal-policy-v6'
PLACEMENT_OPERATOR = 'subgroup-loadout-placement'
PLACEMENT_DECISION = 'incomplete-then-abandon-either-endpoint-then-no-differentiation-then-relative-and-absolute-gain-with-novelty-else-inconclusive-never-adopt'
ALLIED_RACING = 'tower-proposal-racing-v7'
ALLIED_POLICY = 'tower-proposal-policy-v5'
ALLIED_RULE = 'preserve-completable-affinity-endpoints-and-allied-basic-attack-providers-v1'
PRESERVATION_RACING = 'tower-proposal-racing-v6'
PRESERVATION_POLICY = 'tower-proposal-policy-v4'
PRESERVATION_PAIRING = 'same-proposal-root-racing-nomination-and-validation-panels-separate-charges'
REMOVAL_RULE = 'preserve-completable-affinity-endpoints-v1'
VALIDATION_RACING = 'tower-proposal-racing-v5'
VALIDATION_POLICY = 'tower-racing-benchmark-validation-v1'
VALIDATION_PAIRING = 'shared-proposal-root-racing-and-first-16-selection-values-separate-charges'
SELECTOR_RACING = 'tower-proposal-racing-v4'
SELECTOR_POLICY = 'tower-racing-benchmark-positive-tie-v1'
SELECTOR_DECISION = 'incomplete-then-abandon-without-novelty-gate-then-no-observed-output-differentiation-then-larger-fresh-evaluation-warranted-else-inconclusive-never-adopt'
RACING = 'tower-proposal-racing-v2'
CREATION_RACING = 'tower-proposal-racing-v3'
POLICY = 'tower-proposal-policy-v2'
CREATION_POLICY = 'tower-proposal-policy-v3'
SEARCH_FIGHTS = 12672
ROLES = ['wave-1-screen', 'wave-1-continuation', 'wave-2-screen', 'wave-2-continuation', 'selection']
WORK = ContextVar('proposal_audit_work', default=None)


def count(name, amount=1):
    if WORK.get() is not None: WORK.get().add(name, amount)


def raw_read(path):
    return path.read_bytes() if WORK.get() is None else WORK.get().read_bytes(path)


def parse_line(line):
    count('jsonParseAttempts')
    if WORK.get() is not None: count('jsonInputBytes', len(line if isinstance(line, bytes) else line.encode('utf-8')))
    result = json.loads(line)
    count('jsonParseCompleted')
    return result


def require(ok, message):
    if not ok:
        raise ValueError(message)


def fresh_values(version):
    require(version in (VERSION, CREATION_VERSION, SELECTOR_VERSION, VALIDATION_VERSION, PRESERVATION_VERSION, ALLIED_VERSION, PLACEMENT_VERSION, NOMINATION_VERSION), 'Unknown study version')
    return 4380 if version in (PRESERVATION_VERSION, ALLIED_VERSION, PLACEMENT_VERSION, NOMINATION_VERSION) else 4668 if version == VALIDATION_VERSION else 3948


def validation_protocol():
    return dict(valuesPerPair=133, sharedRacingValues=32, controlSelectionValues=40, nominationValues=16, validationValues=60,
        nominationPairing='candidate-nomination-is-first-16-of-control-selection-in-frozen-order',
        validationIsolation='validation-disjoint-from-entire-control-selection-and-all-heldout-panels',
        reporting='all-root-gate-counts-fallback-and-novel-output-frequency-plus-paired-heldout-effects-and-costs')


def preservation_protocol():
    return dict(valuesPerPair=109, sharedRacingValues=32, controlSelectionValues=16, nominationValues=16, validationValues=60,
        nominationPairing='both-arms-share-all-16-nomination-values-in-frozen-order',
        validationIsolation='both-arms-share-60-validation-values-disjoint-from-training-and-all-heldout-panels',
        reporting='both-arm-all-root-gate-counts-fallback-and-novel-output-frequency-plus-paired-heldout-effects-and-costs')


def read(path):
    if WORK.get() is not None:
        # Retain the auditor's existing JSON semantics; accounting is observational.
        with WORK.get().open_read(path) as stream:
            return WORK.get().parse(stream, lambda s:json.loads(s.read().decode('utf-8-sig')))
    return json.loads(path.read_text(encoding='utf-8-sig'))


def storage_reader(root, q, freeze):
    selection = q.get('evidenceStorage')
    if selection is None:
        require(not (root/'evidence-storage.json').exists(), 'Storage descriptor without an explicit request selection')
        return None
    # Load only retained, request-pinned modules; never import from ambient sys.path.
    import importlib.util
    loaded = []
    for name, key in (('proposal_evidence_codec.py', 'codecSha256'), ('proposal_evidence_storage.py', 'readerSha256')):
        path = root/name
        require(not path.is_symlink() and not path.is_junction() and sha(path) == selection[key], 'Changed retained evidence reader module')
        spec = importlib.util.spec_from_file_location(name[:-3], path)
        module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module); loaded.append(module)
    if WORK.get() is not None:
        return loaded[1].Reader(root, selection, freeze, loaded[0], authenticate, work=WORK.get())
    return loaded[1].Reader(root, selection, freeze, loaded[0], authenticate)


def sha(path):
    if WORK.get() is not None: return WORK.get().sha(path)
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def digest(value):
    # Only used for ASCII policy, integer arrays and owner/essence dictionaries.
    # Native reconstruction owns hashes containing .NET-specific numeric encoding.
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=True).encode()).hexdigest()


def close(a, b):
    if isinstance(a, dict) and isinstance(b, dict):
        return a.keys() == b.keys() and all(close(a[k], b[k]) for k in a)
    if isinstance(a, list) and isinstance(b, list):
        return len(a) == len(b) and all(close(x, y) for x, y in zip(a, b))
    if isinstance(a, (int, float)) and not isinstance(a, bool) and isinstance(b, (int, float)) and not isinstance(b, bool):
        return math.isfinite(a) and math.isfinite(b) and math.isclose(a, b, rel_tol=1e-12, abs_tol=1e-12)
    return a == b


def paths(root):
    result = []
    for p in root.rglob('*'):
        require(not p.is_symlink() and not p.is_junction(), 'Linked archive entry')
        if p.is_file():
            result.append(p)
    return result


def authenticate(root, manifest='files.json', exceptions=()):
    files = read(root/manifest)
    actual = {p.relative_to(root).as_posix() for p in paths(root)} - {manifest, *exceptions}
    require(actual == set(files), 'Changed archive membership: '+str(root))
    for name, expected in files.items():
        path = (root/name).resolve()
        require(path.is_relative_to(root.resolve()) and sha(path) == expected, 'Changed file: '+name)
    return files


def classify(entropy, history, version=VERSION):
    count = fresh_values(version)
    require(len(entropy) == 65536 and history == sorted(set(history)) and len(history) <= 983616, 'Changed entropy/history')
    prior, seen, fresh, collisions, duplicates = set(history), set(), [], 0, 0
    for value in struct.unpack('<16384i', entropy):
        if value in prior:
            collisions += 1
        elif value in seen:
            duplicates += 1
        else:
            seen.add(value)
            fresh.append(value)
    return dict(version=version, entropyHash=hashlib.sha256(entropy).hexdigest(), historicalHash=digest(history),
                selected=fresh[:count], reserved=sorted(fresh), historicalCollisions=collisions, duplicates=duplicates)


def score(ids, rows):
    result = []
    for pid in ids:
        outcomes = [o['outcome'] for o in rows if o['request']['partyId'] == pid]
        won = [o for o in outcomes if o['outcome'] == 'Victory']
        result.append(dict(id=pid, samples=len(outcomes), wins=len(won), draws=sum(o['outcome'] == 'Draw' for o in outcomes),
            fitness=dict(worstContextWinRate=len(won)/len(outcomes), guardianHealth=statistics.mean(o['guardianHealth'] for o in outcomes),
                         survival=statistics.mean(o['survival'] for o in outcomes),
                         victoryDuration=statistics.mean(o['durationSeconds'] for o in won) if won else sys.float_info.max)))
    return result


def rank(rows):
    return sorted(rows, key=lambda r: (-r['wins']/r['samples'], r['fitness']['guardianHealth'],
                  -r['fitness']['survival'], r['fitness']['victoryDuration'], r['id']))


def physical_party(context, party):
    physical = copy.deepcopy(context['scope']['contexts'][0]['characterTemplates'])
    for actor in physical:
        actor['build']['essenceIds'] = party['builds'][str(actor['partySlot'])]
        actor['build']['identityEssenceIds'] = [f'neutral-identity-slot-{i+1}' for i in range(context['scope']['budget']['essenceSlots'])]
    return physical


def physical_identity(scenario):
    party = copy.deepcopy(scenario['party'])
    for actor in party:
        build = actor['build']
        build['equipment'].sort(key=lambda e: e['slot'])
        build['identityEssenceIds'] = build.get('identityEssenceIds') or build['essenceIds']
    party.sort(key=lambda p: p['partySlot'])
    # Equality key only; .NET hash serialization is checked in the native audit.
    return json.dumps(party, sort_keys=True, separators=(',', ':'))


def frozen_members(context, outputs, members):
    """Reconstruct native physical deduplication and its first-role representative.

    Provenance can differ when two policies select the same physical recipe.
    Keep the exact control/candidate/benchmark representative, and require every
    role in its physical group; never discard metadata from that representative.
    """
    groups = {}
    for role in ('control','candidate','benchmark'):
        party = physical_party(context, outputs[role])
        identity = physical_identity(dict(party=party))
        group = groups.setdefault(identity, dict(party=outputs[role], physical=party, roles=[]))
        group['roles'].append(role)
    require(len(members) == len(groups), 'Changed physical selected-team groups')
    seen, roles = set(), {}
    for member in members:
        identity = physical_identity(member['scenario'])
        require(identity in groups and identity not in seen, 'Changed physical selected-team group')
        group = groups[identity]; seen.add(identity)
        require(member['party'] == group['party'] and member['scenario']['party'] == group['physical']
            and member['roles'] == sorted(group['roles']), 'Changed frozen selected-team representative or roles')
        for role in member['roles']: roles[role] = member
    return roles


class Archive:
    def __init__(self, root, context):
        self.root, self.context, self.index, self.recipes = root, context, 0, {}
        authenticate(root)
        self.scope = read(root/'scope.json')
        require(self.scope['reportStorage'] == 'gzip-json-v1' and self.scope['contentHashes'] == context['scope']['contentHashes'], 'Changed native content binding')
        for name, pin in self.scope['contentHashes'].items():
            require(sha(root/'content/Data'/name) == pin, 'Changed native captured data')
        raw = raw_read(root/'trials.jsonl')
        require(raw.endswith(b'\n'), 'Torn trial ledger')
        self.trials = [parse_line(line) for line in raw.splitlines()]
        require([t['id'] for t in self.trials] == [f'trial-{i+1:06d}' for i in range(len(self.trials))], 'Changed trial order')

    def consume(self, observation, party):
        request, outcome = observation['request'], observation['outcome']
        require(self.index < len(self.trials), 'Missing native report')
        trial = self.trials[self.index]
        self.index += 1
        require(request['ordinal'] == self.index and trial['id'] == outcome['trialId'] and trial['stage'] == request['role']
                and trial['seed'] == request['seed'] == outcome['seed'] and request['partyId'] == party['id'], 'Changed native request/ledger')
        require(all(re.fullmatch('[0-9a-f]{64}', trial[k]) for k in ('recipe','inputHash','cacheKey')), 'Invalid prepared identity')
        name = trial['recipe']+'.json'
        if name not in self.recipes:
            self.recipes[name] = read(self.root/'recipes'/name)
        scenario = self.recipes[name]
        require(scenario == request['scenario'] and scenario['party'] == physical_party(self.context, party)
                and scenario['floorNumber'] == self.context['scope']['budget']['priorityFloor']
                and scenario['startsAt'] == self.context['scope']['startsAt'], 'Changed physical recipe')
        report_path = self.root/'battles'/(trial['id']+'.json.gz')
        if WORK.get() is None:
            with gzip.open(report_path, 'rt', encoding='utf-8') as stream: report = json.load(stream)
        else:
            work = WORK.get()
            with work.open_read(report_path) as raw, gzip.GzipFile(fileobj=raw, mode='rb') as stream:
                report = work.parse(work.decode_stream(stream), lambda s:json.loads(s.read().decode('utf-8')))
        battle, summary = report['battle'], report['battle']['summary']
        require(battle['schemaVersion'] == 1 and battle['scenarioId'] == scenario['id'] and battle['seed'] == trial['seed']
                and battle['ticksPerSecond'] == 10 and summary['contentOutcome'] in ('Victory','Defeat','Draw')
                and summary['engineOutcome'] in ('Victory','Defeat','Draw') and summary['friendly']
                and summary['durationTicks'] >= 0 and summary['durationSeconds'] == summary['durationTicks']/10
                and report['displayDurationSeconds'] == math.ceil(summary['durationSeconds'])
                and report['succeeded'] == (summary['contentOutcome'] == 'Victory'), 'Invalid literal battle')
        direct = dict(outcome=summary['contentOutcome'], guardianHealth=report['guardianHealthRemainingPercent'],
                      survival=100*sum(p['health'] > 0 for p in summary['friendly'])/len(summary['friendly']), durationSeconds=summary['durationSeconds'])
        require(0 <= direct['guardianHealth'] <= 100 and close({k: outcome[k] for k in direct}, direct), 'Summary differs from native battle')
        count('reconstructedTrialBindings')
        return direct['outcome'] == 'Victory'

    def finish(self):
        require(self.index == len(self.trials), 'Extra native trials')
        require({p.name for p in (self.root/'recipes').iterdir()} == set(self.recipes)
                and {p.name for p in (self.root/'battles').iterdir()} == {t['id']+'.json.gz' for t in self.trials}, 'Changed physical archive membership')


def validate_policies(design):
    fresh_values(design['version'])
    if design['version'] == NOMINATION_VERSION:
        old = copy.deepcopy(design)
        old['version'] = PRESERVATION_VERSION
        old['candidate'] = dict(old['candidate'], version=PRESERVATION_POLICY,
            name='benchmark-preserving-affinity-creation-v4', creationRemovalRule=REMOVAL_RULE)
        require(design['control'] == design['candidate'], 'Nomination must keep the same generator')
        validate_policies(old)
        return
    if design['version'] == PLACEMENT_VERSION:
        # Reuse the exact v5 contract without relaxing older version checks.
        selected = design['control'].get('createdDamageAffinityIds', [])
        require(selected and len(selected) <= 32 and selected == sorted(set(selected))
                and all(re.fullmatch('[0-9a-f]{64}', value) for value in selected), 'Changed selected affinities')
        control = dict(version=ALLIED_POLICY, name='benchmark-allied-action-affinity-creation-v5',
            firstWave=['affinity-create']*9, secondWave=['affinity-create']*8, parentTickets=['benchmark'],
            preserveParentInteractions=False, preservedDamageAffinityIds=[], createdDamageAffinityIds=selected, creationRemovalRule=ALLIED_RULE)
        candidate = dict(version=PLACEMENT_POLICY, name='benchmark-subgroup-loadout-placement-v6',
            firstWave=[PLACEMENT_OPERATOR]*9, secondWave=[PLACEMENT_OPERATOR]*8, parentTickets=['benchmark'], preserveParentInteractions=False)
        require(design['control'] == control and design['candidate'] == candidate, 'Changed versioned policy contrast')
        require(design.get('selectionContrast') == dict(control=VALIDATION_POLICY, candidate=VALIDATION_POLICY)
                and design.get('validationProtocol') == preservation_protocol(), 'Changed placement validation protocol')
        return
    control = dict(version=POLICY, name='benchmark-single-control-v2', firstWave=['single']*9,
                   secondWave=['single']*8, parentTickets=['benchmark'], preserveParentInteractions=False,
                   preservedDamageAffinityIds=[])
    candidate = design['candidate']
    validation = design['version'] == VALIDATION_VERSION
    preservation = design['version'] in (PRESERVATION_VERSION, ALLIED_VERSION)
    selector = design['version'] in (SELECTOR_VERSION, VALIDATION_VERSION, PRESERVATION_VERSION, ALLIED_VERSION)
    creation = design['version'] in (CREATION_VERSION, SELECTOR_VERSION, VALIDATION_VERSION, PRESERVATION_VERSION, ALLIED_VERSION)
    selected = candidate.get('createdDamageAffinityIds' if creation else 'preservedDamageAffinityIds', [])
    require(selected and (not creation or len(selected) <= 32) and selected == sorted(set(selected))
            and all(re.fullmatch('[0-9a-f]{64}', value) for value in selected), 'Changed selected affinities')
    expected = dict(control, name='benchmark-damage-preserving-single-v2', preservedDamageAffinityIds=selected)
    if creation:
        expected = dict(control, version=CREATION_POLICY, name='benchmark-affinity-creation-v3',
                        firstWave=['affinity-create']*9, secondWave=['affinity-create']*8, createdDamageAffinityIds=selected)
    expected_candidate = dict(expected, version=PRESERVATION_POLICY, name='benchmark-preserving-affinity-creation-v4', creationRemovalRule=REMOVAL_RULE) if preservation else expected
    if design['version'] == ALLIED_VERSION:
        expected = expected_candidate
        expected_candidate = dict(expected, version=ALLIED_POLICY, name='benchmark-allied-action-affinity-creation-v5', creationRemovalRule=ALLIED_RULE)
    require(design['control'] == (expected if selector else control) and candidate == expected_candidate, 'Changed versioned policy contrast')
    contrast = dict(control=SELECTOR_POLICY, candidate=VALIDATION_POLICY) if validation else dict(control='tower-staged-incumbent-tie-v1',candidate=SELECTOR_POLICY)
    if preservation: contrast = dict(control=VALIDATION_POLICY, candidate=VALIDATION_POLICY)
    require(design.get('selectionContrast') == (contrast if selector else None),
            'Changed selection contrast')
    require(design.get('validationProtocol') == (preservation_protocol() if preservation else validation_protocol() if validation else None), 'Changed validation allocation protocol')


def select(scores, nominees, primary, benchmark=None):
    positions = {pid:i for i,pid in enumerate(nominees)}
    best = min(scores,key=lambda r:(-r['wins'],r['fitness']['guardianHealth'] if r['wins']==0 else 0,positions[r['id']],r['id']))
    if benchmark is not None and best['wins'] > 0 and next(r for r in scores if r['id']==benchmark)['wins'] == best['wins']:
        return benchmark
    return primary if best['wins'] > 0 and any(r['id']==primary and r['wins']==best['wins'] for r in scores) else best['id']


def selector_trajectories(control, candidate):
    def training(report):
        batches = [dict(b,feedbackPanels=[]) for b in report['batches']]
        e = report['evaluation']
        panels = [dict(p,freeze=dict(p['freeze'],version='',planHash=''),observations=[
            dict(request=dict(o['request'],panelHash=''),outcome=dict(o['outcome'],requestHash='')) for o in p['observations']]) for p in e['panels']]
        return dict(policyHash=report['policyHash'],batches=batches,decisions=e['decisions'],nominees=e['nominees'],panels=panels)
    require(training(control) == training(candidate), 'Selector comparison changed generation or training trajectories')


def validation_trajectories(control, candidate):
    a, b = copy.deepcopy(control), copy.deepcopy(candidate)
    a['evaluation']['panels'] = a['evaluation']['panels'][:4]
    b['evaluation']['panels'] = b['evaluation']['panels'][:4]
    selector_trajectories(a, b)
    selection, nomination = control['evaluation']['panels'][4], candidate['evaluation']['panels'][4]
    require(nomination['freeze']['parties'] == selection['freeze']['parties']
            and nomination['freeze']['seeds'] == selection['freeze']['seeds'][:16], 'Changed paired nomination')
    for row in nomination['observations']:
        other = next(o for o in selection['observations'] if o['request']['partyId'] == row['request']['partyId'] and o['request']['seed'] == row['request']['seed'])
        require(physical_identity(row['request']['scenario']) == physical_identity(other['request']['scenario'])
                and {k:v for k,v in row['outcome'].items() if k not in ('requestHash','trialId')}
                    == {k:v for k,v in other['outcome'].items() if k not in ('requestHash','trialId')}, 'Changed shared nomination observation')


def preservation_trajectories(control, candidate):
    # Proposal membership may change; shared physical requests may not change results.
    def key(row):
        r = row['request']
        return r['role'], r['partyId'], r['seed']
    a = {key(o): o for p in control['evaluation']['panels'] for o in p['observations']}
    for panel in candidate['evaluation']['panels']:
        for row in panel['observations']:
            other = a.get(key(row))
            if other is None: continue
            require(physical_identity(row['request']['scenario']) == physical_identity(other['request']['scenario'])
                and {k:v for k,v in row['outcome'].items() if k not in ('requestHash','trialId')}
                    == {k:v for k,v in other['outcome'].items() if k not in ('requestHash','trialId')}, 'Changed shared proposer observation')


def validation_decision(frozen, panel):
    rows = panel['observations']; gains = losses = 0
    require(panel['complete'] and len(rows) == 120 and len(panel['freeze']['seeds']) == 60, 'Incomplete validation panel')
    for a, b in zip(rows[:60], rows[60:]):
        require(a['request']['seed'] == b['request']['seed'], 'Unpaired validation')
        aw, bw = a['outcome']['outcome'] == 'Victory', b['outcome']['outcome'] == 'Victory'
        gains += aw and not bw; losses += bw and not aw
    numerator = sum(math.comb(gains+losses,k) for k in range(gains,gains+losses+1))
    denominator = 1 << (gains+losses)
    passed = gains > losses and 20*numerator <= denominator
    return dict(version=VALIDATION_POLICY, freezeHash=digest(frozen), panelHash=rows[0]['request']['panelHash'],
        samples=60, gainedWins=gains, lostWins=losses, tailNumerator=numerator, tailDenominator=denominator,
        passed=passed, selectedId=frozen['challengerId'] if passed else frozen['benchmarkId'])


def allied_providers(inventory):
    """Independently derive direct equipped providers; native replay also verifies
    effect-definition hashes with .NET's canonical numeric/string serialization."""
    nodes = {n['key']: n for n in inventory['nodes']}
    require(len(nodes) == len(inventory['nodes']), 'Duplicate allied-action node')
    result = []
    for essence in sorted(inventory['essences'], key=lambda e:e['id']):
        for ability in sorted(set(essence['abilityIds'])):
            key = 'Ability:'+ability
            require(key in nodes and nodes[key]['kind'] == 'Ability' and not nodes[key]['unknowns']
                and nodes[key]['definition']['id'] == nodes[key]['id'] == ability, 'Changed allied-action root')
            for effect in nodes[key]['definition']['effects']:
                effect_key = 'Effect:'+key+'/'+effect['id']
                require(effect_key in nodes and nodes[effect_key]['kind'] == 'Effect' and not nodes[effect_key]['unknowns']
                    and json.dumps(nodes[effect_key]['definition'], sort_keys=True) == json.dumps(effect, sort_keys=True), 'Changed allied-action effect')
                if effect['operation'] == 'PerformBasicAttack' and effect['target'] == 'NonSummonedAllies':
                    result.append(dict(essenceId=essence['id'], abilityNodeKey=key, effectNodeKey=effect_key,
                        operation=effect['operation'], target=effect['target']))
    return sorted({digest(p):p for p in result}.values(), key=lambda p:(p['essenceId'],p['abilityNodeKey'],p['effectNodeKey']))


def creation_step(proposal, parent, policy, providers=None):
    """Check the literal minimal edit. Native replay also authenticates route derivation and RNG."""
    require(len(proposal['scheduledOwners']) == 1 and str(proposal['scheduledOwners'][0]) in parent['builds']
            and proposal['fallback'] is None and proposal['interaction'] is None, 'Changed creation owner/fallback')
    party, step = proposal['party'], proposal.get('affinityCreation')
    allied = policy['version'] == ALLIED_POLICY
    preserving = allied or policy['version'] == PRESERVATION_POLICY
    if party is None:
        require(step is None and proposal['constructionChecks'] == proposal['replacementDistance'] == 0
                and proposal['changedOwners'] == [] and proposal['rejection'] in ('affinities-already-active',
                    'no-legal-allied-action-preserving-affinity-creation' if allied else 'no-legal-preserving-affinity-creation' if preserving else 'no-legal-affinity-creation'),
                'Changed rejected creation')
        return
    owner = str(proposal['scheduledOwners'][0])
    require(step is not None and proposal['constructionChecks'] == 1 and proposal['changedOwners'] == proposal['scheduledOwners']
            and set(party['builds']) == set(parent['builds'])
            and all(party['builds'][o] == parent['builds'][o] for o in parent['builds'] if o != owner), 'Changed creation locality')
    old, new = set(parent['builds'][owner]), set(party['builds'][owner])
    require(step['removed'] == sorted(old-new) and step['added'] == sorted(new-old)
            and len(step['added']) == len(step['removed']) == proposal['replacementDistance'] in (1, 2), 'Changed minimal creation edit')
    pairs = [set(pair) for pair in itertools.combinations(sorted(new), 2) if digest(list(pair)) == step['pairId']]
    require(len(pairs) == 1 and pairs[0]-old == set(step['added']) and not pairs[0].intersection(step['removed']),
            'Creation did not add exactly the missing target endpoints')
    target, active = step['targetAffinityIds'], step['newlyActivatedAffinityIds']
    require(target and active and target == sorted(set(target)) and active == sorted(set(active))
            and set(target) <= set(active) <= set(policy['createdDamageAffinityIds']), 'Changed creation route metadata')
    protection = step.get('removalSelection')
    if preserving:
        require(isinstance(protection, dict) and set(protection) == ({'rule','protectedEssences','eligiblePairs','eligibleEdits'} | ({'alliedActionProtections'} if allied else set()))
            and protection['rule'] == (ALLIED_RULE if allied else REMOVAL_RULE)
            and protection['protectedEssences'] == sorted(set(protection['protectedEssences']))
            and pairs[0] & old <= set(protection['protectedEssences']) <= old
            and not set(protection['protectedEssences']).intersection(step['removed'])
            and type(protection['eligiblePairs']) is int and 1 <= protection['eligiblePairs'] <= len(policy['createdDamageAffinityIds'])
            and type(protection['eligibleEdits']) is int and 1 <= protection['eligibleEdits'] <= math.comb(len(old)-len(protection['protectedEssences']),len(step['added'])),
            'Changed preservation metadata or removed protected endpoint')
        if allied:
            require(providers is not None, 'Missing captured allied-action inventory')
            expected = [p for p in providers if p['essenceId'] in old]
            reasons = protection['alliedActionProtections']
            require(isinstance(reasons, list) and all(isinstance(p, dict) and set(p) == {'essenceId','abilityNodeKey','effectNodeKey','operation','target','effectDefinitionHash'}
                and isinstance(p['effectDefinitionHash'], str) and re.fullmatch('[0-9a-f]{64}', p['effectDefinitionHash']) for p in reasons), 'Changed allied-action evidence shape')
            require([{k:v for k,v in p.items() if k != 'effectDefinitionHash'} for p in reasons] == expected
                and {p['essenceId'] for p in expected} <= set(protection['protectedEssences']), 'Missing or forged allied-action protection')
        # Exact route derivation and eligible-set reconstruction are also checked
        # by the complementary native audit against the captured full inventory.
    else:
        require(protection is None, 'Earlier creation policy acquired preservation metadata')


def native_ascii_digest(value):
    # The frozen placement scope/scenarios are ASCII. Fail closed outside this
    # supported bridge; affinity inventories can contain Unicode and use native reconstruction.
    text = json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=False, allow_nan=False)
    require(text.isascii(), 'Placement hash bridge requires the frozen ASCII scope')
    for char in ['+', '<', '>', '&', "'"]:
        text = text.replace(char, '\\u%04X' % ord(char))
    return hashlib.sha256(text.encode()).hexdigest()


def placement_catalogue(scope, benchmark_reference):
    parent = next(s['party'] for s in scope['starts'] if s['referenceId'] == benchmark_reference)
    anchor = next(r['scenario'] for r in scope['references'] if r['id'] == benchmark_reference)
    require(scope['requiredPartySize'] == 10 and scope['budget']['essenceSlots'] == 5
            and set(parent['builds']) == {str(i) for i in range(1, 11)}, 'Changed placement owners/slots')
    references = {s['party']['id'] for s in scope['starts']}
    recipes, identities, excluded, duplicates = {}, 0, 0, 0
    for subgroup, owners in enumerate((range(1, 6), range(6, 11)), 1):
        for sources in itertools.permutations(owners):
            assignment = dict(sourceByDestination={str(o): s for o, s in zip(owners, sources)})
            builds = copy.deepcopy(parent['builds'])
            for owner, source in zip(owners, sources):
                builds[str(owner)] = list(parent['builds'][str(source)])
            pid = digest(builds)
            if pid == parent['id']:
                identities += 1
            elif pid in references:
                excluded += 1
            elif pid in recipes:
                require(recipes[pid]['party']['builds'] == builds, 'Placement identity collision')
                recipes[pid]['assignments'].append(assignment); duplicates += 1
            else:
                scenario = copy.deepcopy(anchor); scenario['seeds'] = []
                for actor in scenario['party']:
                    actor['build']['essenceIds'] = builds[str(actor['partySlot'])]
                recipes[pid] = dict(party=dict(id=pid, source=PLACEMENT_POLICY, builds=builds), subgroup=subgroup,
                    changedOwners=[o for o in owners if builds[str(o)] != parent['builds'][str(o)]],
                    replacementDistance=sum(len(set(builds[o])-set(parent['builds'][o])) for o in builds),
                    seedFreeScenarioHash=native_ascii_digest(scenario), assignments=[assignment])
    return dict(version='tower-subgroup-loadout-placement-v1', scopeHash=native_ascii_digest(scope), parentId=parent['id'],
        assignmentsExamined=240, identityAssignments=identities, referenceAssignments=excluded, duplicateAssignments=duplicates,
        recipes=[recipes[pid] for pid in sorted(recipes)], interpretation='StructuralPlacementOnlyNoCombatValueOrUptimeClaim')


def placement_step(proposal, catalogue, draw):
    recipe = next((r for r in catalogue['recipes'] if r['party'] == proposal.get('party')), None)
    require(recipe is not None, 'Placement proposal absent from complete catalogue')
    expected = dict(catalogueHash=native_ascii_digest(catalogue), drawOrdinal=draw, subgroup=recipe['subgroup'], assignment=recipe['assignments'][0])
    require(proposal.get('loadoutPlacement') == expected and proposal.get('affinityCreation') is None and proposal.get('interaction') is None
            and proposal.get('fallback') is None and proposal.get('rejection') is None and proposal['constructionChecks'] == 1
            and proposal['scheduledOwners'] == sorted(map(int, recipe['assignments'][0]['sourceByDestination']))
            and proposal['changedOwners'] == recipe['changedOwners'] and proposal['replacementDistance'] == recipe['replacementDistance'],
            'Changed placement provenance')


def search(root, context, design, values, number, arm, catalogue=None, storage=None):
    archive = Archive(root, context)
    plan = read(root/'racing/plan.json')
    report = read(root/'racing/search.json') if storage is None else storage.read(root/'racing', 'search.json')
    racing, evaluation, policy = plan['racing'], report['evaluation'], design[arm]
    validation = design['version'] == VALIDATION_VERSION
    preservation = design['version'] in (PRESERVATION_VERSION, ALLIED_VERSION, PLACEMENT_VERSION, NOMINATION_VERSION)
    placement = design['version'] == PLACEMENT_VERSION and arm == 'candidate'
    validate_output = preservation or validation and arm == 'candidate'
    selector = design['version'] in (SELECTOR_VERSION, VALIDATION_VERSION, PRESERVATION_VERSION, ALLIED_VERSION)
    opt_in = selector and arm == 'candidate' or validation and arm == 'control'
    creation = selector or design['version'] == CREATION_VERSION and arm == 'candidate'
    racing_version, policy_version = (CREATION_RACING, CREATION_POLICY) if creation else (RACING, POLICY)
    if opt_in: racing_version = SELECTOR_RACING
    if validate_output: racing_version = VALIDATION_RACING
    if preservation and arm == 'candidate': racing_version, policy_version = PRESERVATION_RACING, PRESERVATION_POLICY
    if design['version'] == ALLIED_VERSION:
        racing_version, policy_version = (PRESERVATION_RACING, PRESERVATION_POLICY) if arm == 'control' else (ALLIED_RACING, ALLIED_POLICY)
    if design['version'] == PLACEMENT_VERSION:
        creation = not placement
        racing_version, policy_version = (PLACEMENT_RACING, PLACEMENT_POLICY) if placement else (ALLIED_RACING, ALLIED_POLICY)
    if design['version'] == NOMINATION_VERSION:
        creation = True
        racing_version, policy_version = (VALIDATION_RACING if arm == 'control' else NOMINATION_RACING), CREATION_POLICY
    require(plan['version'] == report['version'] == evaluation['version'] == racing_version and plan['policy'] == policy
            and plan.get('selectionPolicyVersion') == report.get('selectionPolicyVersion') == (VALIDATION_POLICY if validate_output else SELECTOR_POLICY if opt_in else None)
            and archive.scope['algorithm'] == racing_version+'/'+report['planHash']
            and plan.get('damageAffinityInventory') == (None if placement else context['damageAffinityInventory']) and racing['mechanics'] == context['mechanics']
            and report['policyHash'] == digest(policy) and evaluation['status'] == 'Complete'
            and evaluation['chargedEvaluations'] == evaluation['plannedEvaluations'] == racing['maximumEvaluations'] == 528
            and len(report['batches']) == len(evaluation['decisions']) == 2 and len(evaluation['panels']) == (6 if validate_output else 5),
            'Changed policy/search contract')
    width = 109 if preservation else 133 if validation else 73
    offset = (number-1)*width
    roles = ROLES[:4]+['nomination','validation'] if validate_output else ROLES
    panel_seeds = [values[offset+1+i*8:offset+1+i*8+(40 if i==4 else 8)] for i in range(5)]
    if validate_output: panel_seeds = panel_seeds[:4]+[values[offset+33:offset+49],values[offset+73:offset+133]]
    if preservation: panel_seeds = panel_seeds[:4]+[values[offset+33:offset+49],values[offset+49:offset+109]]
    require(racing['rootSeed'] == values[offset] and racing['benchmarkReferenceId'] == context['benchmarkReferenceId']
            and racing['panels'] == [dict(role=r, seeds=s) for r,s in zip(roles,panel_seeds)], 'Changed paired search seeds')
    scope = copy.deepcopy(context['scope'])
    scope['generation']['seeds'] = [values[offset]]
    scope['excludedCombatSeeds'] = sorted(set(scope['excludedCombatSeeds']) | (set(values)-set(values[offset:offset+width])))
    require(racing['scope'] == scope, 'Changed pair context/history')
    if placement:
        require(catalogue == placement_catalogue(scope, context['benchmarkReferenceId'])
                and len(catalogue['recipes']) >= 17, 'Changed complete placement catalogue')
    parties = {s['party']['id']: s['party'] for s in context['scope']['starts']}
    references, seen = list(parties), set(parties)
    benchmark = next(s['party'] for s in context['scope']['starts'] if s['referenceId'] == context['benchmarkReferenceId'])
    primary = next(s['party']['id'] for s in context['scope']['starts'] if s['referenceId'] == context['scope']['stages']['selectionPrimaryReferenceId'])
    providers = allied_providers(context['damageAffinityInventory']) if design['version'] in (ALLIED_VERSION, PLACEMENT_VERSION) else None
    beam, previous, before, charges, inputs = [], [], 0, [], []
    allowed = {e['id']: e['family'] for e in scope['allowedEssences']}
    for i,panel in enumerate(evaluation['panels']):
        freeze, rows = panel['freeze'], panel['observations']
        if i in (0,2):
            batch = report['batches'][i//2]
            require(batch == read(root/f'racing/batch-{i//2+1:02d}.json') and batch['wave'] == i//2+1
                    and batch['afterEvaluations'] == before and batch['beamIds'] == beam and set(batch['seenBefore']) == seen
                    and batch['feedbackPanels'] == [p['observations'][0]['request']['panelHash'] for p in evaluation['panels'][:i]]
                    and len(batch['candidates']) == (9 if i==0 else 8) and len(batch['proposals']) <= 128, 'Changed adaptive batch')
            accepted = []
            if placement: require(len(batch['proposals']) == (9 if i == 0 else 8), 'Changed fixed placement draw count')
            proposal_seen = set(seen)
            for j,p in enumerate(batch['proposals']):
                require(p['attempt'] == j+1 and p['requestedOperator'] == p['effectiveOperator'] == (PLACEMENT_OPERATOR if placement else 'affinity-create' if creation else 'single')
                        and p['parents'] == [benchmark['id']] and p['parentSource'] == 'benchmark'
                        and 0 <= p['constructionChecks'] <= 32, 'Changed proposal lineage/bounds')
                if creation:
                    creation_step(p, benchmark, policy, providers)
                if placement:
                    placement_step(p, catalogue, (0 if i == 0 else 9)+j+1)
                if p['party'] is not None:
                    if creation:
                        require(p['rejection'] == ('duplicate-recipe' if p['party']['id'] in proposal_seen else None), 'Changed creation duplicate handling')
                        proposal_seen.add(p['party']['id'])
                    if p['rejection'] is None:
                        accepted.append(p['party'])
                    changed = [int(o) for o in benchmark['builds'] if benchmark['builds'][o] != p['party']['builds'][o]]
                    distance = sum(len(set(p['party']['builds'][o])-set(benchmark['builds'][o])) for o in benchmark['builds'])
                    require(changed == p['changedOwners'] and distance == p['replacementDistance']
                            and (distance > 0 if placement else distance in ((1, 2) if creation else (1,))), 'Changed edit distance')
            require(accepted == batch['candidates'], 'Changed accepted proposals')
            for party in batch['candidates']:
                require(party['id'] == digest(party['builds']) and party['id'] not in seen and party['source'] == policy_version
                        and set(party['builds']) == {str(n+1) for n in range(scope['requiredPartySize'])}, 'Duplicate or illegal candidate')
                for ids in party['builds'].values():
                    require(ids == sorted(set(ids)) and len(ids) == scope['budget']['essenceSlots']
                            and all(e in allowed for e in ids) and len({allowed[e] for e in ids}) == len(ids), 'Illegal essence family placement')
                if scope['ownedCopies'] is not None:
                    flat = [e for ids in party['builds'].values() for e in ids]
                    require(all(flat.count(e) <= scope['ownedCopies'].get(e,0) for e in set(flat)), 'Owned-copy bound exceeded')
                seen.add(party['id']); parties[party['id']] = party
            ids = references+beam+[p['id'] for p in batch['candidates']]
        elif i in (1,3):
            ids = references+evaluation['decisions'][i//2]['survivorIds']
        elif i == 4:
            eligible = set(references) | set(beam[:2])
            ids = [r['id'] for r in rank(evaluation['decisions'][1]['commonScores']) if r['id'] in eligible]
            require(evaluation['nominees'] == ids, 'Changed nominees')
        else:
            excluded = set(references) if design['version'] == NOMINATION_VERSION and arm == 'candidate' else {benchmark['id']}
            nonbenchmark = [r for r in nomination_scores if r['id'] not in excluded]
            challenger = select(nonbenchmark, [pid for pid in evaluation['nominees'] if pid not in excluded], primary)
            frozen = dict(version=VALIDATION_POLICY, planHash=report['planHash'],
                nominationPanelHash=evaluation['panels'][4]['observations'][0]['request']['panelHash'], challengerId=challenger, benchmarkId=benchmark['id'])
            require(evaluation['validationFreeze'] == read(root/'racing/validation-freeze.json') == frozen, 'Changed validation challenger freeze')
            ids = [challenger, benchmark['id']]
        require(panel['complete'] and freeze == read(root/f'racing/panel-{i+1:02d}.json') and freeze['parties'] == [parties[pid] for pid in ids]
                and freeze['index'] == i and freeze['role'] == roles[i] and freeze['version'] == racing_version
                and freeze['planHash'] == report['planHash'] and freeze['seeds'] == panel_seeds[i]
                and freeze['evaluationsBefore'] == before and freeze['plannedEvaluations'] == len(rows) == len(ids)*len(panel_seeds[i]), 'Changed racing freeze')
        for pid in ids:
            for seed in panel_seeds[i]:
                observation = rows[len(charges)-before]
                request = observation['request']
                require(request['role'] == roles[i] and request['seed'] == seed and request['scenario']['seeds'] == panel_seeds[i], 'Changed panel membership/order')
                archive.consume(observation, parties[pid])
                charges.append(dict(ordinal=len(charges)+1, panelHash=request['panelHash'], partyId=pid, seed=seed))
                inputs.append(archive.trials[len(charges)-1])
        computed = score(ids,rows)
        require(close(panel['scores'],computed), 'Changed panel fitness')
        if i == 4: nomination_scores = computed
        contrasts = []
        wins = {pid: {o['request']['seed']: o['outcome']['outcome']=='Victory' for o in rows if o['request']['partyId']==pid} for pid in ids}
        for pid in ids:
            for ref in references:
                if pid != ref and ref in ids:
                    contrasts.append(dict(partyId=pid, referenceId=ref, samples=len(panel_seeds[i]),
                        gainedWins=sum(wins[pid][s] and not wins[ref][s] for s in panel_seeds[i]),
                        lostWins=sum(not wins[pid][s] and wins[ref][s] for s in panel_seeds[i])))
        require(panel['contrasts'] == contrasts, 'Changed reference contrasts')
        if i in (0,2):
            d = evaluation['decisions'][i//2]; ranked = rank([r for r in computed if r['id'] not in references])
            elite = [r['id'] for r in ranked[:3]]; cutoff = ranked[2]['wins']-1
            def distance(pid):
                return min(sum(len(set(parties[pid]['builds'][o])-set(parties[e]['builds'][o])) for o in parties[pid]['builds']) for e in elite)
            eligible = [r for r in ranked[3:] if r['wins'] >= cutoff]
            diverse = sorted(eligible,key=lambda r:-distance(r['id']))[0] if eligible else ranked[3]
            survivors = elite+[diverse['id']]
            require(d['eliteIds'] == elite and d['survivorIds'] == survivors and d['diversityId'] == diverse['id']
                    and d['competitiveCutoffWins'] == cutoff and d['diversityFallback'] == (not eligible)
                    and d['diversityDistance'] == distance(diverse['id']) and d['prunedIds'] == [r['id'] for r in ranked if r['id'] not in survivors], 'Changed pruning/diversity')
        elif i in (1,3):
            common = score(ids,previous+rows); beam = [r['id'] for r in rank(common) if r['id'] not in references]
            require(close(evaluation['decisions'][i//2]['commonScores'],common) and evaluation['decisions'][i//2]['beamIds'] == beam, 'Changed real-feedback beam')
        previous = rows; before += len(rows)
    if validate_output:
        expected_decision = validation_decision(frozen, evaluation['panels'][5])
        saved = evaluation['validationDecision']
        require(all(type(saved[k]) is int for k in ('samples','gainedWins','lostWins','tailNumerator','tailDenominator'))
                and type(saved['passed']) is bool and saved == read(root/'racing/validation-decision.json') == expected_decision,
                'Changed exact validation gate')
        chosen = expected_decision['selectedId']
        members = {'plan.json','search.json','charges.jsonl','inputs.jsonl','batch-01.json','batch-02.json','validation-freeze.json','validation-decision.json'}
        members.update(f'panel-{n:02d}.json' for n in range(1,7))
        if storage is not None: members = storage.physical_members(root/'racing', members)
        require({p.name for p in (root/'racing').iterdir()} == members, 'Changed validation archive membership')
    else:
        chosen = select(evaluation['panels'][-1]['scores'], evaluation['nominees'], primary, benchmark['id'] if opt_in else None)
        if storage is not None:
            members = {'plan.json','search.json','charges.jsonl','inputs.jsonl','batch-01.json','batch-02.json'} | {f'panel-{n:02d}.json' for n in range(1,6)}
            require({p.name for p in (root/'racing').iterdir()} == storage.physical_members(root/'racing', members), 'Changed encoded racing membership')
    require(evaluation['rawSelectedId'] == chosen and before == 528, 'Changed selected output')
    for name,expected in [('charges.jsonl',charges),('inputs.jsonl',inputs)]:
        raw = raw_read(root/'racing'/name); require(raw.endswith(b'\n') and [parse_line(l) for l in raw.splitlines()] == expected, 'Changed durable journal')
    archive.finish()
    count('reconstructedTrajectories')
    return report, parties[chosen], archive


def interval(values):
    mean = statistics.mean(values); sd = statistics.stdev(values); margin = 2.200985160082949*sd/math.sqrt(12)
    return dict(mean=mean,standardDeviation=sd,lower=mean-margin,upper=mean+margin)


def decision(method, benchmark, differing, promising, version=VERSION):
    fresh_values(version)
    if version in (PLACEMENT_VERSION, NOMINATION_VERSION):
        if method <= -.02 or benchmark <= -.02: return 'AbandonThisConfiguration'
        if differing == 0: return 'NoObservedOutputDifferentiation'
        return 'LargerFreshEvaluationWarranted' if method >= .02 and benchmark >= .02 and differing >= 3 and promising >= 3 else 'Inconclusive'
    selector = version in (SELECTOR_VERSION, VALIDATION_VERSION, PRESERVATION_VERSION, ALLIED_VERSION)
    if method <= -.02 or benchmark <= -.02 and (selector or promising < 3):
        return 'AbandonThisConfiguration'
    if differing == 0:
        return 'NoObservedOutputDifferentiation'
    return 'LargerFreshEvaluationWarranted' if method >= .02 and benchmark >= 0 and differing >= 3 and (selector or promising >= 3) else 'Inconclusive'


def audit_resources(root, q, working):
    version = q.get('resourceEnvelope')
    require(q.get('version') not in (ALLIED_VERSION, PLACEMENT_VERSION, NOMINATION_VERSION) or version == 'tower-proposal-resource-envelope-v2',
            'This comparison requires the frozen v2 resource envelope')
    require(version in (None, 'tower-proposal-resource-envelope-v1', 'tower-proposal-resource-envelope-v2'),
            'Unknown proposal resource envelope')
    native, audit_seconds = (9000, 1800) if version == 'tower-proposal-resource-envelope-v2' else (9600, 1200)
    if not (root/'launch.json').exists():
        require(working and version is None, 'Missing owned launch envelope')
        return  # Original literal row fixtures have no process wrapper.
    launch = read(root/'launch.json')
    start = dt.datetime.fromisoformat(launch['startedAt'])
    require(launch['version'] == q['version'] and launch['requestFileHash'] == sha(root/'request.json')
            and launch['maximumSeconds'] == 10800 and launch['maximumBytes'] == 6442450944
            and launch['nativeMaximumSeconds'] == native and launch['nativeMaximumBytes'] == 5905580032
            and dt.datetime.fromisoformat(launch['nativeDeadline']) == start+dt.timedelta(seconds=native)
            and dt.datetime.fromisoformat(launch['deadline']) == start+dt.timedelta(seconds=10800),
            'Changed owned resource envelope')
    receipt = read(root/'admission-receipt.json')
    require(receipt['version'] == q['version'], 'Changed admitted study version')
    if version is not None:
        require(receipt.get('resourceEnvelope') == version, 'Changed admitted resource envelope')
    if working:
        return
    completion, final = read(root/'completion.json'), read(root/'closeout.json')
    def bounded(value, limit):
        return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value) and 0 <= value < limit
    require(completion['version'] == final['version'] == q['version'] and completion['status'] == 'Complete' and completion['retries'] == 0
            and bounded(completion['nativeSeconds'], native) and bounded(completion['auditSeconds'], audit_seconds)
            and bounded(completion['seconds'], 10800) and bounded(completion['nativeBytes'], 5905580032)
            and bounded(completion['auditBytes'], 536870912) and bounded(completion['observedBytes'], 6442450944)
            and math.isclose(completion['seconds'], completion['nativeSeconds']+completion['auditSeconds'], abs_tol=1e-6)
            and completion['observedBytes'] == completion['nativeBytes']+completion['auditBytes']
            and completion['chargedSeconds'] == completion['seconds'] and completion['chargedBytes'] == completion['observedBytes'],
            'Changed cumulative completion accounting')
    require(bounded(final['measuredSeconds'], 10800) and bounded(final['auditSeconds'], audit_seconds)
            and bounded(final['retainedBytes'], 6442450944) and bounded(final['auditBytes'], 536870912)
            and final['measuredSeconds'] >= completion['seconds'] and final['retainedBytes'] >= completion['observedBytes']
            and math.isclose(final['measuredSeconds'], completion['nativeSeconds']+final['auditSeconds'], abs_tol=1e-6)
            and final['retainedBytes'] == completion['nativeBytes']+final['auditBytes']
            and final['chargedSeconds'] == final['measuredSeconds'] and final['chargedBytes'] == final['retainedBytes'],
            'Changed terminal resource receipt')


def audit(root, working=False, pin=None, work=None):
    token = WORK.set(work)
    try: return _audit(root, working, pin)
    finally: WORK.reset(token)


def _audit(root, working=False, pin=None):
    root = root.resolve()
    if not working:
        require(pin is not None and sha(root/'closeout.json') == pin, 'Supply the external closeout pin')
        require(read(root/'closeout.json')['filesHash'] == sha(root/'files.json'), 'Changed final manifest')
        authenticate(root, exceptions=('closeout.json',))
    require(not (root/'failure.json').exists(), 'Incomplete study')
    q = read(root/'request.json'); fresh_values(q['version'])
    version = q['version']
    validation = version == VALIDATION_VERSION
    placement = version == PLACEMENT_VERSION
    strict_gain = placement or version == NOMINATION_VERSION
    preservation = version in (PRESERVATION_VERSION, ALLIED_VERSION, PLACEMENT_VERSION, NOMINATION_VERSION)
    selector = version in (SELECTOR_VERSION, VALIDATION_VERSION, PRESERVATION_VERSION, ALLIED_VERSION)
    audit_resources(root, q, working)
    for key in ('plan','context','settings','history','runtime'):
        require(sha(root/'source'/(key+'.json')) == q[key]['sha256'], 'Changed frozen source: '+key)
    require(sha(root/'auditor.py') == q['auditor']['sha256'], 'Changed independent auditor')
    design, context, history = read(root/'source/plan.json'), read(root/'source/context.json'), read(root/'source/history.json')
    require(design['version'] == version and design['roots'] == 12 and design['heldoutSamples'] == 256
            and design['maximumFights'] == 21888 and design['requiredFreshValues'] == fresh_values(version) and design['searchFightsPerArm'] == 528
            and design['maximumSeconds'] == 10800 and design['maximumBytes'] == 6442450944, 'Changed fixed design')
    require(design['pairing'] == (PRESERVATION_PAIRING if preservation else VALIDATION_PAIRING if validation else 'same-proposal-root-and-search-panels-separate-charges')
            and design['barrier'] == 'freeze-all-24-outputs-before-any-heldout-observation'
            and design['interpretation'] == 'development-pilot-no-efficacy-or-adoption-claim'
            and design['analysis'] == dict(
                primaryEndpoint='equal-root-mean-heldout-candidate-minus-control-win-rate',
                guardrailEndpoint='equal-root-mean-heldout-candidate-minus-fixed-benchmark-win-rate',
                identicalOutputs='reuse-one-physical-recipe-per-root-heldout-panel-exact-zero-method-contrast-retain-absolute-benchmark-results',
                rootRetention='all-12-roots-no-preview-screening-no-replacement-no-refill-any-incomplete-root-invalidates-study',
                rootUncertainty='report-all-12-paired-root-contrasts-and-descriptive-95-percent-t-df11-not-a-powered-efficacy-test',
                conditionalUncertainty='paired-seed-standard-errors-and-covariance-conditional-on-frozen-outputs-not-future-root-reliability',
                goMethodAtLeast=.02,goBenchmarkAtLeast=.02 if strict_gain else 0,goDifferingRootsAtLeast=3,goPromisingNovelRootsAtLeast=0 if selector else 3,
                promisingNovelGainAtLeast=.03,abandonMethodAtMost=-.02,abandonBenchmarkAtMost=-.02,abandonPromisingNovelRootsBelow=0 if selector or strict_gain else 3,
                decisionOrder=PLACEMENT_DECISION if strict_gain else SELECTOR_DECISION if selector else 'incomplete-then-abandon-then-no-observed-output-differentiation-then-larger-fresh-evaluation-warranted-else-inconclusive-never-adopt'),
            'Changed prospective analysis')
    validate_policies(design)
    allocation = classify(raw_read(root/'entropy.bin'),history,version)
    require(allocation == read(root/'allocation.json') and len(allocation['selected']) == fresh_values(version), 'Changed allocation')
    require(read(root/'history-input.json') == dict(reservationState='Complete',reserved=allocation['reserved'])
            and read(root/'seed-ledger.json') == dict(reservationState='Complete',historical=history,reserved=allocation['reserved']), 'Lost reservation tail')
    require(read(root/'entropy-intent.json') == dict(version=version,words=16384,assignedValues=fresh_values(version),historicalHash=digest(history),retries=0), 'Changed entropy intent')
    values = allocation['selected']; authenticate(root/'study')
    freeze = read(root/'study/freeze.json'); summary = read(root/'study/summary.json')
    storage = storage_reader(root, q, freeze)
    require(freeze['version'] == version and freeze['valuesHash'] == digest(values) and freeze['planHash'] == digest(design)
            and [f['root'] for f in freeze['families']] == list(range(1,13)), 'Changed all-root barrier')
    require(read(root/'study/binding.json') == dict(version=version, planHash=freeze['planHash'],
            contextHash=freeze['contextHash'], valuesHash=freeze['valuesHash']), 'Changed study version binding')
    heldout = Archive(root/'heldout',context); roots = []; validation_decisions = []; control_validation_decisions = []; expected_files = {'files.json','binding.json','freeze.json','summary.json'}
    require(heldout.scope['algorithm'] == version+'/heldout/'+summary['freezeHash'], 'Changed held-out algorithm')
    references = {physical_identity(dict(party=physical_party(context,s['party']))) for s in context['scope']['starts']}
    benchmark = next(s['party'] for s in context['scope']['starts'] if s['referenceId']==context['benchmarkReferenceId'])
    require(benchmark['id'] == design['benchmarkPartyId'], 'Changed fixed benchmark')
    for number,family in enumerate(freeze['families'],1):
        outputs, reports = {}, {}
        catalogue = None
        if placement:
            name = f'placement-catalogue-{number:02d}.json'; expected_files.add(name)
            catalogue = read(root/'study'/name)
        for arm in ('control','candidate'):
            report, party, archive = search(root/f'search/root-{number:02d}'/arm,context,design,values,number,arm,catalogue,storage)
            outputs[arm],reports[arm] = party,report
            require(archive.scope['settings'] == heldout.scope['settings'] and archive.scope['execution'] == heldout.scope['execution'], 'Changed paired execution/settings')
        pair_name = f'pair-{number:02d}.json'
        pair = read(root/'study'/pair_name) if storage is None else storage.read(root/'study',pair_name)
        expected_files.add(pair_name)
        require(pair['status'] == 'Complete' and pair['planHash'] == freeze['planHash'] and pair['control'] == reports['control'] and pair['candidate'] == reports['candidate'], 'Changed pair output')
        if version == NOMINATION_VERSION:
            a, b = copy.deepcopy(reports['control']), copy.deepcopy(reports['candidate'])
            a['evaluation']['panels'] = a['evaluation']['panels'][:5]
            b['evaluation']['panels'] = b['evaluation']['panels'][:5]
            selector_trajectories(a, b)
        if version == SELECTOR_VERSION: selector_trajectories(reports['control'], reports['candidate'])
        if validation:
            validation_trajectories(reports['control'], reports['candidate'])
        if preservation:
            preservation_trajectories(reports['control'], reports['candidate'])
            control_validation_decisions.append(reports['control']['evaluation']['validationDecision'])
        if validation or preservation:
            validation_decisions.append(reports['candidate']['evaluation']['validationDecision'])
        outputs['benchmark'] = benchmark
        start = 12*(109 if preservation else 133 if validation else 73)
        seeds = values[start+(number-1)*256:start+number*256]
        require(family['seeds'] == seeds and 1 <= len(family['members']) <= 3, 'Changed held-out panel')
        selected_members = frozen_members(context, outputs, family['members'])
        identities, role_rows = set(), {}
        require([m['recipeHash'] for m in family['members']] == sorted(m['recipeHash'] for m in family['members']), 'Reordered held-out recipes')
        for member in family['members']:
            identity = physical_identity(member['scenario'])
            require(identity not in identities and member['roles'] == sorted(set(member['roles'])) and member['scenario']['seeds'] == seeds, 'Duplicate physical held-out recipe')
            identities.add(identity)
            for role in member['roles']:
                require(role in outputs and role not in role_rows
                        and member['scenario']['party'] == physical_party(context,outputs[role]), 'Changed frozen selected team')
            name = f"heldout-{number:02d}-{member['recipeHash']}.json"; expected_files.add(name)
            cell = read(root/'study'/name) if storage is None else storage.read(root/'study',name)
            require(cell['root'] == number and cell['recipeHash'] == member['recipeHash'] and len(cell['observations']) == 256, 'Changed held-out evidence')
            wins = []
            for seed,observation in zip(seeds,cell['observations']):
                request = observation['request']
                require(request['role'] == f'heldout-root-{number:02d}' and request['seed'] == seed
                        and request['scenario'] == member['scenario'] and request['panelHash'] == summary['freezeHash'], 'Held-out leakage or membership change')
                wins.append(heldout.consume(observation,member['party']))
            for role in member['roles']:
                role_rows[role] = wins
            count('reconstructedHeldoutMembers')
        require(set(role_rows) == {'control','candidate','benchmark'}, 'Missing output role')
        count('reconstructedRoots')
        if placement: count('reconstructedCatalogues')
        a,b,r = [role_rows[k] for k in ('control','candidate','benchmark')]
        d = [int(x)-int(y) for x,y in zip(b,a)]; g = [int(x)-int(y) for x,y in zip(b,r)]
        dm,gm = statistics.mean(d),statistics.mean(g)
        cov = lambda x,y,xm,ym: sum((xx-xm)*(yy-ym) for xx,yy in zip(x,y))/(256*255)
        roots.append(dict(root=number,controlParty=selected_members['control']['party']['id'],candidateParty=selected_members['candidate']['party']['id'],
            benchmarkParty=selected_members['benchmark']['party']['id'],
            identical=selected_members['control'] is selected_members['candidate'],
            novel=physical_identity(selected_members['candidate']['scenario']) not in references,
            changedPositionsPerWave=[sum(x['id']!=y['id'] for x,y in zip(aa['candidates'],bb['candidates'])) for aa,bb in zip(reports['control']['batches'],reports['candidate']['batches'])],
            controlWins=sum(a),candidateWins=sum(b),benchmarkWins=sum(r),methodGains=sum(x>0 for x in d),methodLosses=sum(x<0 for x in d),
            benchmarkGains=sum(x>0 for x in g),benchmarkLosses=sum(x<0 for x in g),methodDifference=dm,benchmarkDifference=gm,
            methodSeedStandardError=math.sqrt(cov(d,d,dm,dm)),benchmarkSeedStandardError=math.sqrt(cov(g,g,gm,gm)),seedCovariance=cov(d,g,dm,gm)))
    heldout.finish()
    if storage is not None:
        expected_files = storage.physical_members(root/'study', expected_files)
        storage.finish()
    require({p.name for p in (root/'study').iterdir()} == expected_files, 'Unexpected study evidence')
    method,benchmark_interval = interval([r['methodDifference'] for r in roots]),interval([r['benchmarkDifference'] for r in roots])
    differing,novel,promising = sum(not r['identical'] for r in roots),sum(r['novel'] for r in roots),sum(r['novel'] and r['benchmarkDifference']>=.03 for r in roots)
    expected = dict(version=version,status='Verified',decision=decision(method['mean'],benchmark_interval['mean'],differing,promising,version),planHash=freeze['planHash'],freezeHash=summary['freezeHash'],
        fights=SEARCH_FIGHTS+heldout.index,searchFights=SEARCH_FIGHTS,heldoutFights=heldout.index,differingRoots=differing,novelRoots=novel,promisingNovelRoots=promising,
        roots=roots,method=method,benchmark=benchmark_interval,methodMedian=statistics.median(r['methodDifference'] for r in roots),
        methodWorst=min(r['methodDifference'] for r in roots),benchmarkMedian=statistics.median(r['benchmarkDifference'] for r in roots),
        benchmarkWorst=min(r['benchmarkDifference'] for r in roots),interpretation='development-pilot-no-efficacy-or-adoption-claim')
    provisional = read(root/'provisional-result.json')
    if validation or preservation:
        expected['validation'] = dict(passedRoots=sum(d['passed'] for d in validation_decisions),
            fallbackRoots=sum(not d['passed'] for d in validation_decisions), decisions=validation_decisions)
        require(summary['validation'] == provisional['validation'] == expected['validation'], 'Changed exact validation diagnostics')
    if preservation:
        expected['controlValidation'] = dict(passedRoots=sum(d['passed'] for d in control_validation_decisions),
            fallbackRoots=sum(not d['passed'] for d in control_validation_decisions), decisions=control_validation_decisions)
        require(summary['controlValidation'] == provisional['controlValidation'] == expected['controlValidation'], 'Changed control validation diagnostics')
    require(close(summary,expected) and close(provisional,expected) and expected['fights']<=21888, 'Changed endpoint/decision')
    raw = raw_read(root/'attempts.jsonl'); lines = raw.splitlines(keepends=True)
    require(raw.endswith(b'\n') and len(lines)==2*expected['fights'] and hashlib.sha256(b''.join(lines[:2*SEARCH_FIGHTS])).hexdigest()==freeze['attemptsHash'], 'Changed barrier/attempt accounting')
    for i,line in enumerate(lines):
        require(parse_line(line)==dict(kind='Completed' if i%2 else 'Started',ordinal=i//2+1), 'Changed attempt order')
    # Return the saved native values only after independent numeric agreement.
    # Publication normalizes JSON number spelling and requires exact typed values.
    count('reconstructedStudyEndpoints')
    return dict(status='Passed',requestFileHash=sha(root/'request.json'),newFights=0,newValues=0,result=summary)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', nargs='?')
    parser.add_argument('--working')
    parser.add_argument('--pin')
    parser.add_argument('--output')
    parser.add_argument('--work-binding', help='Optional external diagnostic worker binding')
    parser.add_argument('--work-binding-pin', help='External SHA-256 of that binding')
    args = parser.parse_args(argv)
    if bool(args.work_binding) != bool(args.work_binding_pin): parser.error('Supply both work binding and pin')
    root = Path(args.working or args.archive)
    def execute(work=None):
        result = audit(root, bool(args.working), args.pin, work=work)
        if args.output:
            if work is not None: work.write_json_output(args.output, result)
            else:
                with Path(args.output).open('x',encoding='utf-8') as stream:
                    json.dump(result,stream,indent=2)
        else:
            print(json.dumps(result,indent=2))
    if args.work_binding:
        from proposal_work_accounting import worker_receipt
        with worker_receipt(root, 'independentAudit', args.work_binding, args.work_binding_pin, __file__) as work:
            execute(work)
    else:
        execute()


if __name__ == '__main__': main()
