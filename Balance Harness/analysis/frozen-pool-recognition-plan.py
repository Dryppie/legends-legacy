"""Freeze a recognition diagnostic from the closed adaptive pilot.

Commands separate catalogue publication, one recorded sampling draw, deterministic
plan building from that external receipt, and read-only verification. They never
invoke the simulator, prepare combat inputs or reserve combat values.
"""
import argparse
import copy
import hashlib
import importlib.util
from itertools import combinations
import json
from pathlib import Path
import secrets
import time

ROOT = Path(__file__).resolve().parents[2]
REVIEW = ROOT/'TestResults/adaptive-racing-stage-review-20260923'
REVIEW_PIN = 'f4087410b3647ef1294c732f0570459daeb901913617502216345828a2508835'
VERSION = 'tower-frozen-pool-recognition-plan-v1'
SAMPLING = 'one-batch-uniform-unordered-pair-v1'
ENTROPY_BYTES = 128
PAIRS = list(combinations(range(13), 2))
SAMPLES = 256


def require(ok, message):
    if not ok:
        raise ValueError(message)


def digest(value):
    return sha(json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=True, allow_nan=False).encode())


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding='utf-8'))


def save(path, value):
    with path.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(value, stream, indent=2, ensure_ascii=True, allow_nan=False)
        stream.write('\n')


def seal(output):
    save(output/'files.json', {p.name: sha(p.read_bytes()) for p in sorted(output.iterdir()) if p.is_file()})
    return sha((output/'files.json').read_bytes())


def authenticated(root, pin):
    require(sha((root/'files.json').read_bytes()) == pin, 'Changed package manifest')
    manifest = read(root/'files.json')
    require(set(p.name for p in root.iterdir()) == set(manifest) | {'files.json'}, 'Changed package membership')
    for name, expected in manifest.items():
        require(Path(name).name == name and (root/name).is_file() and not (root/name).is_symlink(), 'Invalid package member')
        require(sha((root/name).read_bytes()) == expected, 'Changed package member: '+name)
    return manifest


def source_tools():
    authenticated(REVIEW, REVIEW_PIN)
    spec = importlib.util.spec_from_file_location('recognition_source_review', REVIEW/'analysis.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def sampling_design():
    return dict(version=SAMPLING, roots=list(range(1, 13)), populationPerRoot=13, sampledPerRoot=2,
        populationOrder='Ascending full partyId using ordinal ASCII order',
        pairs='Lexicographic itertools.combinations(range(13), 2); zero-based index 0..77',
        entropyBytes=ENTROPY_BYTES, acceptByteBelow=234, pairIndex='acceptedByte % 78',
        traversal='One byte stream, roots 1..12; reject bytes 234..255; next accepted byte completes that root',
        individualInclusion=dict(numerator=2, denominator=13), unorderedPairProbability=dict(numerator=1, denominator=78),
        totalEstimatorWeight=dict(numerator=13, denominator=2),
        originAssumption='The one post-catalogue OS CSPRNG draw is modeled as independent uniform bytes; a hash alone cannot prove entropy provenance.',
        failure='Persist the incomplete attempt; no refill, redrawing, root replacement or reuse of the same output directory.',
        unusedBytes='Retain the entire batch including rejected bytes and the unused tail; never recycle it for combat.',
        combatValuesAllocated=0)


def physical_context(scenario):
    result = copy.deepcopy(scenario)
    result['seeds'] = []
    for actor in result['party']:
        actor['build']['essenceIds'] = []
    return result


def validate_recipe(team, context, allowed, slots):
    scenario = team['scenario']
    require(scenario['seeds'] == [] and physical_context(scenario) == context, 'Changed physical context or combat seeds')
    require([a['partySlot'] for a in scenario['party']] == list(range(1, 11)), 'Changed owner order')
    builds = {str(a['partySlot']): a['build']['essenceIds'] for a in scenario['party']}
    require(digest(builds) == team['partyId'] and digest(scenario) == team['seedFreeScenarioHash'], 'Changed recipe identity')
    for ids in builds.values():
        require(len(ids) == slots and ids == sorted(set(ids)) and all(x in allowed for x in ids), 'Illegal or noncanonical Essence list')
        require(len({allowed[x].casefold() for x in ids}) == slots, 'Repeated source-monster family within an owner')


def root_catalogue(number, report, plan, references):
    e, scope = report['evaluation'], plan['scope']
    require(e['status'] == 'Complete' and e['chargedEvaluations'] == 528 and len(report['batches']) == 2
            and [s['party']['id'] for s in scope['starts']] == references and scope['ownedCopies'] is None, 'Changed captured search scope')
    generated = [p for batch in report['batches'] for p in batch['candidates']]
    parties = {p['id']: p for p in [s['party'] for s in scope['starts']]+generated}
    require(len(generated) == 17 and len(parties) == 20, 'Incomplete or duplicate source population')
    require(len(e['nominees']) == len(set(e['nominees'])) == 5 and set(references).issubset(e['nominees']),
            'Incomplete final nominee set or missing control')
    nominees = [pid for pid in e['nominees'] if pid not in references]
    beam = e['decisions'][-1]['beamIds']
    near = [pid for pid in beam if pid not in nominees]
    lower = sorted(set(parties)-set(references)-set(beam))
    require(len(nominees) == len(set(nominees)) == len(near) == 2 and len(beam) == len(set(beam)) == 4
            and set(nominees).issubset(beam) and len(lower) == 13
            and set(references+nominees+near+lower) == set(parties), 'Changed diagnostic strata')
    # The producing archive already validated this complete physical template.
    # Preserve every actor/gear/identity field; replace only composition and seeds.
    anchor = copy.deepcopy(e['panels'][0]['observations'][0]['request']['scenario'])
    anchor['seeds'] = []
    context = physical_context(anchor)
    allowed = {x['id']: x['family'] for x in scope['allowedEssences']}
    rows = []
    for stratum, ids in [('reference', references), ('nominee', nominees), ('near-miss', near), ('lower', lower)]:
        for ordinal, pid in enumerate(ids, 1):
            scenario = copy.deepcopy(anchor)
            for actor in scenario['party']:
                actor['build']['essenceIds'] = copy.deepcopy(parties[pid]['builds'][str(actor['partySlot'])])
            team = dict(partyId=pid, stratum=stratum, stratumOrdinal=ordinal,
                scenario=scenario, seedFreeScenarioHash=digest(scenario))
            validate_recipe(team, context, allowed, scope['budget']['essenceSlots'])
            rows.append(team)
    return dict(root=number, sourcePlanHash=report['planHash'],
        sourceReport=f'study/pair-{number:02d}-candidate-adaptive.json',
        benchmarkPartyId=references[2], physicalContextHash=digest(context), teams=rows)


def catalogue():
    source = source_tools()
    evidence = source.Evidence(source.RUN, source.RUN_PIN)
    close = source.Evidence(source.CLOSEOUT, source.CLOSEOUT_PIN)
    result = evidence.read('result.json')
    receipt = close.read('closeout.json')
    require(receipt['status'] == 'AdaptiveRacingPilotExecutionVerified' and receipt['archiveManifestSha256'] == source.RUN_PIN
            and receipt['result'] == result == evidence.read('independent-audit.json')['result']
            and receipt['nativeAudit'] == receipt['independentAudit'] == 'Passed'
            and result['decision'] == 'AbandonThisConfiguration', 'Incomplete source study')
    template = evidence.read('template.json')
    references = [s['party']['id'] for s in template['starts']]
    require(set(references) == source.REFERENCES and references[0] == source.PRIMARY and references[2] == source.BENCHMARK, 'Changed controls')
    roots = [root_catalogue(j, evidence.read(f'study/pair-{j:02d}-candidate-adaptive.json'),
                           evidence.read(f'study/pair-{j:02d}-candidate-plan.json'), references) for j in range(1, 13)]
    evidence.recheck(); close.recheck()
    return dict(version=VERSION, status='PopulationFrozenBeforeSampling', runnableRequest=False,
        membership='All 12 source roots; every generated recipe; strata from frozen training membership only. Held-out scores do not determine membership.',
        sources=dict(scientificManifestSha256=source.RUN_PIN, stageReviewManifestSha256=REVIEW_PIN,
            closeoutManifestSha256=source.CLOSEOUT_PIN, consumedScientificFiles=evidence.consumed, consumedCloseoutFiles=close.consumed),
        capturedRuntime=dict(contentHashes=template['contentHashes'], settingsHash=template['settingsHash'], executionHash=template['executionHash']),
        sampling=sampling_design(), samplesPerTeam=SAMPLES, roots=roots)


def publish_catalogue(output):
    require(not output.exists(), 'Catalogue output already exists')
    started = time.monotonic()
    value = catalogue()
    output.mkdir(parents=True)
    save(output/'catalogue.json', value)
    (output/'builder.py').write_bytes(Path(__file__).read_bytes())
    save(output/'completion.json', dict(status='CatalogueFrozen', seconds=time.monotonic()-started, newFights=0, newCombatValues=0))
    pin = seal(output)
    return dict(status='CatalogueFrozen', manifestSha256=pin, catalogueSha256=sha((output/'catalogue.json').read_bytes()), roots=12, recipes=240)


def load_catalogue(package, pin):
    authenticated(package, pin)
    value = read(package/'catalogue.json')
    require(value == catalogue(), 'Catalogue does not reproduce from the frozen source')
    return value, sha((package/'catalogue.json').read_bytes())


def choices(value, catalogue_hash, entropy):
    require(value['sampling'] == sampling_design() and len(entropy) == ENTROPY_BYTES, 'Changed sampling design or entropy batch')
    cursor, rows = 0, []
    for root in value['roots']:
        population = [t['partyId'] for t in root['teams'] if t['stratum'] == 'lower']
        require(root['root'] == len(rows)+1 and len(population) == len(set(population)) == 13 and population == sorted(population), 'Changed sampling population')
        rejected = []
        while cursor < len(entropy) and entropy[cursor] >= 234:
            rejected.append(cursor); cursor += 1
        require(cursor < len(entropy), 'Sampling batch exhausted; no refill or partial plan')
        index = entropy[cursor] % 78
        ids = [population[k] for k in PAIRS[index]]
        rows.append(dict(root=root['root'], populationHash=digest(population), rejectedByteOffsets=rejected,
            acceptedByteOffset=cursor, pairIndex=index, partyIds=ids))
        cursor += 1
    require(len(rows) == 12, 'Incomplete sampled roots')
    return dict(version=SAMPLING, catalogueSha256=catalogue_hash, entropySha256=sha(entropy),
        roots=rows, consumedBytes=cursor, unusedBytes=len(entropy)-cursor,
        individualInclusion=dict(numerator=2, denominator=13), pairProbability=dict(numerator=1, denominator=78))


def sample(package, pin, output, draw=None):
    require(not output.exists(), 'Sampling output already exists; no redraw')
    value, catalogue_hash = load_catalogue(package, pin)
    output.mkdir(parents=True)
    save(output/'intent.json', dict(version=SAMPLING, catalogueManifestSha256=pin, catalogueSha256=catalogue_hash,
        catalogueSourceSha256=sha((package/'builder.py').read_bytes()), samplerSourceSha256=sha(Path(__file__).read_bytes()),
        entropyBytes=ENTROPY_BYTES, draws=1, origin='OperatingSystemCSPRNG' if draw is None else 'LiteralEngineeringFixture', newCombatValues=0))
    (output/'sampler.py').write_bytes(Path(__file__).read_bytes())
    try:
        entropy = (draw or secrets.token_bytes)(ENTROPY_BYTES)
        with (output/'entropy.bin').open('xb') as stream:
            stream.write(entropy)
        selected = choices(value, catalogue_hash, entropy)
        save(output/'choices.json', selected)
        save(output/'completion.json', dict(status='SamplingComplete', newFights=0, newCombatValues=0, entropyBytes=ENTROPY_BYTES, draws=1))
    except Exception as error:
        save(output/'failure.json', dict(status='SamplingFailedNoPlan', error=str(error), noRetry=True))
        raise
    return dict(status='SamplingComplete', manifestSha256=seal(output), entropyBytes=ENTROPY_BYTES, newCombatValues=0)


def verify_sampling(value, catalogue_hash, catalogue_pin, package, pin):
    manifest = authenticated(package, pin)
    require(set(manifest) == {'intent.json', 'sampler.py', 'entropy.bin', 'choices.json', 'completion.json'},
            'Incomplete or failed sampling package')
    intent, completion = read(package/'intent.json'), read(package/'completion.json')
    require(intent['version'] == SAMPLING and intent['catalogueSha256'] == catalogue_hash
            and intent['catalogueManifestSha256'] == catalogue_pin and intent['draws'] == 1
            and intent['entropyBytes'] == ENTROPY_BYTES and intent['origin'] == 'OperatingSystemCSPRNG'
            and intent['samplerSourceSha256'] == sha((package/'sampler.py').read_bytes()) == sha(Path(__file__).read_bytes())
            and completion == dict(status='SamplingComplete', newFights=0, newCombatValues=0, entropyBytes=ENTROPY_BYTES, draws=1), 'Changed sampling provenance')
    expected = choices(value, catalogue_hash, (package/'entropy.bin').read_bytes())
    require(read(package/'choices.json') == expected, 'Externally supplied choice differs from the recorded uniform draw')
    return expected


def build_plan(value, selection, catalogue_hash, sampling_pin):
    require(value['version'] == VERSION and selection['catalogueSha256'] == catalogue_hash
            and len(value['roots']) == len(selection['roots']) == 12, 'Changed catalogue or sampling binding')
    roots = []
    for population, choice in zip(value['roots'], selection['roots']):
        require(population['root'] == choice['root'] == len(roots)+1, 'Reordered roots')
        lower = [t['partyId'] for t in population['teams'] if t['stratum'] == 'lower']
        require(choice['populationHash'] == digest(lower) and type(choice['pairIndex']) is int and 0 <= choice['pairIndex'] < 78
                and choice['partyIds'] == [lower[k] for k in PAIRS[choice['pairIndex']]], 'Changed lower-stratum sampling choice')
        selected = [copy.deepcopy(t) for t in population['teams'] if t['stratum'] != 'lower' or t['partyId'] in choice['partyIds']]
        require(len(selected) == len({t['partyId'] for t in selected}) == 9, 'Missing, duplicated or additional measured team')
        for team in selected:
            team['inclusionProbability'] = dict(numerator=2, denominator=13) if team['stratum'] == 'lower' else dict(numerator=1, denominator=1)
        roots.append(dict(root=population['root'], benchmarkPartyId=population['benchmarkPartyId'],
            physicalContextHash=population['physicalContextHash'], teams=selected,
            unmeasured=[dict(partyId=pid, stratum='lower', independentOutcome=None) for pid in lower if pid not in choice['partyIds']]))
    return dict(version=VERSION, status='FrozenDesignNativeIntegrationRequired', runnableRequest=False,
        catalogueSha256=catalogue_hash, samplingManifestSha256=sampling_pin, sources=value['sources'], capturedRuntime=value['capturedRuntime'],
        roots=roots, samplesPerTeam=SAMPLES, requiredFreshCombatValues=12*SAMPLES, maximumAttemptedFights=12*9*SAMPLES,
        allocation=dict(order='Root, frozen team order, common seed order', withinRoot='One 256-value common panel for all nine teams',
            acrossRoots='Twelve disjoint panels, excluded from complete live history; identical recipes in different roots are not deduplicated',
            samplingRandomness='Population-sampling bytes are not combat seeds and must not be reused as combat entropy'),
        reporting=dict(rates=108, candidateReferenceContrasts=216, approximateWilsonFamily=540,
            intervalFamily='108 recipe rates plus 432 gain/loss rates for 216 signed paired comparisons; reuse established approximate family-adjusted Wilson arithmetic',
            benchmark='The fixed strongest reference 96b94357, not the panel-wise best reference',
            strata='Report nominees, near misses and sampled lower candidates separately, for every root',
            lowerPopulationMean='The mean of the two sampled gains estimates the finite 13-candidate stratum mean; use inclusion weight 13/2 for totals',
            fullCandidateMean='For each frozen root: (sum of four measured nominee/near-miss gains + (13/2)*sum of two sampled lower gains)/17',
            limitations='Lower-stratum sampling and combat uncertainty are separate. No unsampled outcome imputation, raw-pool unweighted average, post-hoc best-team adoption or future-root reliability claim.',
            interpretation='DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion', decision='CompleteDiagnosticOnly'),
        requiredBeforeExecution=['A new native protocol for 12 root-specific nine-team families; the current eight-team fixed-family profiles reject this plan',
            'Captured-runtime compatibility, complete live reservation history and a fresh admitted request',
            'Explicit cumulative elapsed-time/storage envelope covering admission, owned execution, both audits and publication',
            'One post-freeze combat entropy batch, permanent reservation of every exposed fresh value, no retries or replacement roots',
            'Native reconstruction and independent saved-battle audit of all 27,648 attempts and every reported stratum'],
        policyDefaultsChanged=False, newFights=0, newCombatValues=0)


def publish_plan(catalogue_package, catalogue_pin, sampling_package, sampling_pin, output):
    require(not output.exists(), 'Plan output already exists')
    value, catalogue_hash = load_catalogue(catalogue_package, catalogue_pin)
    selection = verify_sampling(value, catalogue_hash, catalogue_pin, sampling_package, sampling_pin)
    require(read(sampling_package/'intent.json')['catalogueSourceSha256'] == sha((catalogue_package/'builder.py').read_bytes()), 'Changed catalogue producer')
    plan = build_plan(value, selection, catalogue_hash, sampling_pin)
    authenticated(catalogue_package, catalogue_pin); authenticated(sampling_package, sampling_pin)
    output.mkdir(parents=True)
    save(output/'plan.json', plan)
    save(output/'bindings.json', dict(cataloguePackage=str(catalogue_package.resolve()), catalogueManifestSha256=catalogue_pin,
        samplingPackage=str(sampling_package.resolve()), samplingManifestSha256=sampling_pin))
    (output/'builder.py').write_bytes(Path(__file__).read_bytes())
    return dict(status=plan['status'], manifestSha256=seal(output), planSha256=sha((output/'plan.json').read_bytes()), measuredRecipes=108, unmeasuredRecipes=132)


def verify_plan(package, pin):
    manifest = authenticated(package, pin)
    require(set(manifest) == {'plan.json', 'bindings.json', 'builder.py'}, 'Changed plan package shape')
    binding = read(package/'bindings.json')
    c, s = Path(binding['cataloguePackage']), Path(binding['samplingPackage'])
    value, catalogue_hash = load_catalogue(c, binding['catalogueManifestSha256'])
    selection = verify_sampling(value, catalogue_hash, binding['catalogueManifestSha256'], s, binding['samplingManifestSha256'])
    require(read(s/'intent.json')['catalogueSourceSha256'] == sha((c/'builder.py').read_bytes()), 'Changed catalogue producer')
    require(sha((package/'builder.py').read_bytes()) == sha(Path(__file__).read_bytes())
            and read(package/'plan.json') == build_plan(value, selection, catalogue_hash, binding['samplingManifestSha256']), 'Plan does not reproduce')
    authenticated(c, binding['catalogueManifestSha256']); authenticated(s, binding['samplingManifestSha256']); authenticated(package, pin)
    return dict(status='VerifiedFrozenRecognitionPlan', newFights=0, newCombatValues=0)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    p = sub.add_parser('catalogue'); p.add_argument('--output', type=Path, required=True)
    for command in ('sample', 'build'):
        p = sub.add_parser(command)
        p.add_argument('--catalogue', type=Path, required=True); p.add_argument('--catalogue-pin', required=True)
        p.add_argument('--output', type=Path, required=True)
        if command == 'build':
            p.add_argument('--sampling', type=Path, required=True); p.add_argument('--sampling-pin', required=True)
    p = sub.add_parser('verify'); p.add_argument('--package', type=Path, required=True); p.add_argument('--manifest-pin', required=True)
    args = parser.parse_args()
    if args.command == 'catalogue': result = publish_catalogue(args.output)
    elif args.command == 'sample': result = sample(args.catalogue, args.catalogue_pin, args.output)
    elif args.command == 'build': result = publish_plan(args.catalogue, args.catalogue_pin, args.sampling, args.sampling_pin, args.output)
    else: result = verify_plan(args.package, args.manifest_pin)
    print(json.dumps(result, indent=2))
