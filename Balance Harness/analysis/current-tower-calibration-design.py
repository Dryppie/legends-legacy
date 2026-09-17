"""Static, seed-free inventory and calibration design. No native execution or allocator.

The mapped keys are Python input identities, never native prepared-participant hashes.
Historical order is retained by source reference; projections use one fixed canonical order.
"""
import copy
from collections import Counter
from datetime import datetime, timezone
import gzip
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'TestResults/current-tower-calibration-design-20260917'
ADMISSION = ROOT/'TestResults/fixed-team-confirmation-admission-20260917-03'
FIXED = ROOT/'TestResults/balance/tower-practical-fixed-team-confirmation-20260917'
OLD = ROOT/'TestResults/balance/tower-retained-family-audit-20260914'
INPUTS = {}


def require(ok, message):
    if not ok:
        raise ValueError(message)


def sha(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()


def rel(path):
    return path.relative_to(ROOT).as_posix()


def read(path):
    INPUTS[rel(path)] = sha(path)
    with (gzip.open(path, 'rt', encoding='utf-8') if path.suffix == '.gz'
          else path.open(encoding='utf-8-sig')) as f:
        return json.load(f)


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':'),
                                     ensure_ascii=False, allow_nan=False).encode()).hexdigest()


def write(path, value):
    with path.open('x', encoding='utf-8', newline='\n') as f:
        json.dump(value, f, indent=2, ensure_ascii=False, allow_nan=False)
        f.write('\n')


def scenario_nodes(value, pointer='$'):
    if isinstance(value, dict):
        if {'schemaVersion','floorNumber','party','preparationState','startsAt'} <= value.keys():
            yield pointer, value
            return
        for k,x in value.items():
            if k not in ('seeds','excludedCombatSeeds','trials','evidence','contentHashes','requiredHistory'):
                yield from scenario_nodes(x, pointer+'/'+k)
    elif isinstance(value, list):
        for i,x in enumerate(value):
            yield from scenario_nodes(x, pointer+'/'+str(i))


def neutral_shape(party):
    return [{**p, 'build': {k:v for k,v in p['build'].items()
                            if k not in ('essenceIds','identityEssenceIds')}} for p in party]


def make_inventory():
    definition = read(FIXED/'source-definition.json')
    target = copy.deepcopy(definition['teams'][0]['scenario'])
    target['id'] = 'current-kharad-calibration-20260917'
    target['seeds'] = []
    settings = read(ADMISSION/'settings-and-execution.json')
    source_pins = read(ADMISSION/'source-files.json')
    drift = []
    for name, pin in source_pins.items():
        INPUTS[name] = sha(ROOT/name)
        if INPUTS[name] != pin:
            drift.append(name)
    content_drift = []
    content_root = ROOT/'LL/src/API/API.LL/Data'
    for name,pin in definition['contentHashes'].items():
        p = content_root/name
        INPUTS[rel(p)] = sha(p)
        if INPUTS[rel(p)] != pin:
            content_drift.append(name)
    require(not drift and not content_drift, 'Current source/content differs from confirmed baseline; redesign scope.')
    essences = {e['id']:e['sourceMonsterId'].casefold()
                for e in read(content_root/'essences/essences.json')['essences']}
    entries, cells, counts = [], {}, Counter()
    sources, unresolved = [], []
    expected_identity = target['party'][0]['build']['identityEssenceIds']

    def add(s, origin, anchor_reason=None):
        s = copy.deepcopy(s)
        s['seeds'] = []
        original_hash = digest(s)
        reasons = []
        party = sorted(s.get('party', []), key=lambda p:p.get('partySlot', -1))
        if s.get('schemaVersion') != 1 or s.get('floorNumber') != 5:
            reasons.append('different-floor-or-schema')
        try:
            instant = datetime.fromisoformat(s['startsAt']).astimezone(timezone.utc).isoformat()
        except (KeyError, ValueError):
            instant = None
        if instant != '2000-01-01T00:00:00+00:00' or s.get('preparationState') != target['preparationState']:
            reasons.append('different-time-or-preparation')
        if neutral_shape(party) != neutral_shape(target['party']):
            reasons.append('different-character-id-equipment-or-budget')
        vectors = []
        for p in party:
            b = p['build']; ids = b.get('essenceIds', [])
            if len(ids) != 5 or any(x not in essences for x in ids):
                reasons.append('unknown-essence-or-slot-count')
            elif len({essences[x] for x in ids}) != 5:
                reasons.append('duplicate-source-creature')
            if b.get('identityEssenceIds', ids) != expected_identity:
                reasons.append('different-character-identity')
            vectors.append(sorted(ids))
        reasons = sorted(set(reasons))
        changed_order = any(p['build']['essenceIds'] != ids for p,ids in zip(party,vectors))
        cell_key = None
        if not reasons:
            # Intentional prospective projection, not equivalence to historical ordered combat.
            cell_key = digest({'targetContext': digest(neutral_shape(target['party'])), 'essencesBySlot': vectors})
            if cell_key not in cells:
                cells[cell_key] = {'inputKey':cell_key, 'essencesBySlot':vectors, 'entryOrdinals':[],
                    'requiredControlIds':[], 'anchorReasons':[], 'nativeAdmission':'Pending', 'nativeParticipantHash':None}
            cells[cell_key]['entryOrdinals'].append(len(entries))
            if anchor_reason and anchor_reason not in cells[cell_key]['anchorReasons']:
                cells[cell_key]['anchorReasons'].append(anchor_reason)
        row = {'ordinal':len(entries), 'source':origin, 'seedFreeHistoricalScenarioHash':original_hash,
               'historicalScenarioId':s.get('id'), 'historicalStartsAt':s.get('startsAt'),
               'inputKey':cell_key, 'classification':'StaticCompatibleProjection' if not reasons else 'Incompatible',
               'reasons':reasons, 'canonicalOrderChangesHistoricalInput':changed_order,
               'anchorReason':anchor_reason}
        entries.append(row)
        counts[row['classification']] += 1
        counts['orderChanged'] += changed_order
        for reason in reasons:
            counts[reason] += 1

    inventory = OLD/'retained-family.json.gz'
    require(sha(inventory) == '8173a6535d12dacdfd6e71eda939520dfeabb7e4399eae7db7d98837feb21fcf',
            'Historical full inventory changed.')
    historical = read(inventory)
    require(len(historical) == 43879, 'Historical inventory incomplete.')
    focused_path = ROOT/'TestResults/balance/tower-focused-challenger-run-20260915/study/source.json'
    focused = read(focused_path)
    focused_keys = {c['inventoryKey'] for c in focused['cells']}
    require(len(focused_keys) == 989, 'Incomplete historical focused family.')
    for i,row in enumerate(historical):
        add(row['scenario'], {'file':rel(inventory), 'pointer':'$/'+str(i), 'key':row['key'],
                              'origins':row['sources']},
            'historical-focused-family' if row['key'] in focused_keys else None)
    historical_origins = sum(len(row['sources']) for row in historical)
    del historical

    metadata = json.loads((OUT/'metadata-candidates.json').read_text())
    # Discovery proposal reconstruction uses its actual saved definition and templates.
    for name in metadata:
        path = ROOT/name
        value = read(path)
        before = len(entries)
        for pointer,s in scenario_nodes(value):
            add(s, {'file':name, 'pointer':pointer}, 'later-saved-scenario')
        generation = value.get('generation',value) if isinstance(value,dict) else {}
        if isinstance(generation,dict) and isinstance(generation.get('arms'),list):
            d = None
            definition_path = None
            context_path = path
            if path.parent.name == 'tower-refinement-role-validity-20260916':
                original = ROOT/'TestResults/balance/tower-refinement-comparison-study-20260916/run/discovery-1/discovery.json'
                require(sha(path) == sha(original), 'Copied role-validity discovery differs from its recorded origin.')
                INPUTS[rel(original)] = sha(original)
                context_path = original
            for base in [context_path.parent, context_path.parent.parent, context_path.parent.parent.parent]:
                for filename in ['definition.json','source-definition.json']:
                    candidate = base/filename
                    if candidate.exists():
                        possible = read(candidate)
                        if isinstance(possible,dict) and 'contexts' in possible and 'budget' in possible:
                            d, definition_path = possible, candidate
                            break
                if d is not None:
                    break
            if d is None:
                unresolved.append({'source':name,'reason':'No saved discovery context definition'})
            else:
                for arm_i,arm in enumerate(generation['arms']):
                    breaches = {e['id'] for e in arm.get('evaluations',[]) if
                        any(sum(c.get('clears',[]))*2 > len(c.get('clears',[]))
                            for c in e.get('cells',[]))}
                    for proposal_i,proposal in enumerate(arm['proposals']):
                        choice = proposal.get('party')
                        if not choice:
                            continue
                        for context in d['contexts']:
                            party = copy.deepcopy(context['characterTemplates'])
                            for member in party:
                                member['build']['essenceIds'] = choice['builds'].get(str(member['partySlot']), [])
                                member['build']['identityEssenceIds'] = [f'neutral-identity-slot-{n}'
                                    for n in range(1,d['budget']['essenceSlots']+1)]
                            s = {'schemaVersion':1,'id':d['id'],'floorNumber':d['budget']['priorityFloor'],
                                'startsAt':d['startsAt'],'preparationState':'uncleared-no-contributions',
                                'assumptions':target['assumptions'],'seeds':[],'party':party}
                            add(s, {'file':name,'definition':rel(definition_path), 'arm':arm_i,
                                'proposal':proposal_i,'partyId':choice['id'], 'context':context['id'],
                                'historicalResult':proposal.get('result')},
                                'later-discovery-ceiling-observation' if choice['id'] in breaches else None)
        sources.append({'file':name,'sha256':INPUTS[name],'scenarioOccurrences':len(entries)-before})

    for team in definition['teams']:
        before = len(entries)
        add(team['scenario'], {'file':rel(FIXED/'source-definition.json'), 'requiredControl':team['partyId']},
            'confirmed-fixed-team-control')
        key = entries[before]['inputKey']
        require(key is not None, 'Required control is statically incompatible.')
        cells[key]['requiredControlIds'].append(team['partyId'])
    result = {'version':'current-tower-calibration-inventory-v1','status':'StaticInventoryCompleteNativeAdmissionPending',
        'historicalEntries':43879,'historicalOrigins':historical_origins, 'metadataFiles':len(sources),
        'sourceOccurrences':len(entries), 'counts':dict(counts), 'projectedInputCells':len(cells),
        'exactProjectionAliases':counts['StaticCompatibleProjection']-len(cells),
        'forcedSecondStageInputCells':sum(bool(c['anchorReasons']) for c in cells.values()),
        'incompatibleAnchorOccurrences':sum(e['anchorReason'] is not None and e['inputKey'] is None for e in entries),
        'unresolvedDiscoverySources':unresolved, 'requiredControls':3,
        'sourceDrift':drift,'contentDrift':content_drift,
        'execution':settings['execution'], 'executionHash':definition['executionHash'],
        'settingsHash':definition['settingsHash'],'contentHashes':definition['contentHashes'],
        'nativePreparedEquivalenceEstablished':False,'fullCurrentGameplayFamilyFrozen':False,
        'newFights':0,'newSeeds':0,'nativePreparations':0}
    return result, entries, sorted(cells.values(),key=lambda x:x['inputKey']), sources, target


def generate():
    result, entries, cells, sources, target = make_inventory()
    for name,rows in [('entries.jsonl.gz',entries),('cells.jsonl.gz',cells)]:
        with (OUT/name).open('xb') as raw, gzip.GzipFile(fileobj=raw,mode='wb',mtime=0,filename='') as f:
            for row in rows:
                f.write((json.dumps(row,sort_keys=True,separators=(',',':'),ensure_ascii=False)+'\n').encode())
    write(OUT/'sources.json', sources)
    write(OUT/'target-template.json',target)
    write(OUT/'inventory-summary.json',result)
    write(OUT/'input-pins.json',dict(sorted(INPUTS.items())))
    print(json.dumps({k:v for k,v in result.items() if k not in ('contentHashes','execution')},indent=2))


def verify():
    expected = json.loads((OUT/'input-pins.json').read_text())
    for name,h in expected.items():
        require(sha(ROOT/name) == h, 'Changed static input: '+name)
    result, entries, cells, sources, target = make_inventory()
    require(result == json.loads((OUT/'inventory-summary.json').read_text()), 'Summary differs.')
    require(sources == json.loads((OUT/'sources.json').read_text()), 'Source catalogue differs.')
    require(target == json.loads((OUT/'target-template.json').read_text()), 'Target template differs.')
    require(INPUTS == expected, 'Input catalogue differs.')
    for name,rows in [('entries.jsonl.gz',entries),('cells.jsonl.gz',cells)]:
        with gzip.open(OUT/name,'rt',encoding='utf-8') as f:
            for row in rows:
                require(json.loads(next(f)) == row, 'Reproduced inventory row differs: '+name)
            require(not f.read(), 'Extra inventory row: '+name)
    manifests = {}
    provenance = json.loads((OUT/'source-provenance.json').read_text())
    require({s['file'] for s in sources} == {s['file'] for s in provenance}, 'Provenance coverage mismatch.')
    for row in provenance:
        require(row['manifestBindings'] and sha(ROOT/row['file']) == row['sha256'], 'Unbound source.')
        for binding in row['manifestBindings']:
            path = ROOT/binding['manifest']
            require(sha(path) == binding['sha256'], 'Changed source manifest.')
            if path not in manifests:
                manifests[path] = json.loads(path.read_text(encoding='utf-8-sig'))
            require(manifests[path][binding['member']] == row['sha256'], 'Manifest membership mismatch.')
    runtime = json.loads((ADMISSION/'runtime-files.json').read_text())
    for name,h in runtime.items():
        require(sha(ADMISSION/'runtime'/name) == h, 'Changed confirmed runtime.')
    return {'status':'Passed','entries':len(entries),'projectedCells':len(cells),
            'sourceManifestsChecked':len(manifests),'boundMetadataFiles':len(provenance),
            'runtimeFiles':len(runtime),'sourcePins':len(expected),'newFights':0,'newSeeds':0}


if __name__ == '__main__':
    require(sys.argv[1:] in (['generate'],['verify']), 'Use generate or read-only verify.')
    if sys.argv[1:] == ['generate']:
        generate()
    else:
        print(json.dumps(verify(),indent=2))
