"""Freeze a paired-pool recognition plan; no simulator, combat entropy or reservation.

Catalogue, one population-sampling draw, plan publication and deterministic
verification are separate operations. Old recognition profiles remain unchanged.
"""
import argparse
import copy
from fractions import Fraction
import hashlib
import importlib.util
from itertools import combinations
import json
from pathlib import Path
import secrets
import time

ROOT = Path(__file__).resolve().parents[2]
REVIEW = ROOT/'TestResults/affinity-preservation-stage-review-20260924'
REVIEW_PIN = 'dc09dc54929ec14403804e6cc6d97c1af632fcb708eb11ba9d0c512d7ccf5abd'
HELPER = ROOT/'TestResults/affinity-creation-recognition-catalogue-20260924'
HELPER_PIN = 'ccd05f08fda2c86ab014c12ca056d061053683d7b47c69cafee6902a08831814'
VERSION = 'tower-affinity-preservation-recognition-plan-v1'
SOURCE_VERSION = 'tower-affinity-preservation-comparison-v1'
SELECTOR = 'tower-racing-benchmark-validation-v1'
RACING = dict(control='tower-proposal-racing-v5', candidate='tower-proposal-racing-v6')
ARMS = ('control', 'candidate')
STRATA = ('remaining-shared', 'remaining-control-only', 'remaining-candidate-only')
SAMPLING = 'paired-pool-uniform-stratified-subsets-v1'
ENTROPY_BYTES, SAMPLES = 256, 256


def require(ok, message):
    if not ok:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


# Reuse only pure identity/package helpers from the sealed prior builder.
# Authenticate before importing; no old catalogue, sampler or plan logic is used.
require(sha((HELPER/'files.json').read_bytes()) == HELPER_PIN, 'Changed helper manifest')
HELPER_SOURCE_PIN = json.loads((HELPER/'files.json').read_bytes())['builder.py']
require(sha((HELPER/'builder.py').read_bytes()) == HELPER_SOURCE_PIN, 'Changed helper source')
base = module('preservation_recognition_helpers', HELPER/'builder.py')
digest, read, save, seal = base.digest, base.read, base.save, base.seal
authenticated, physical_context, validate_recipe = base.authenticated, base.physical_context, base.validate_recipe


def source_tools():
    authenticated(REVIEW, REVIEW_PIN)
    return module('preservation_recognition_source', REVIEW/'affinity-preservation-stage-review.py')


def fraction(n, d):
    f = Fraction(n, d)
    return dict(numerator=f.numerator, denominator=f.denominator)


def stratum_design(ids):
    n, k = len(ids), min(2, len(ids))
    return dict(populationCount=n, sampleCount=k, populationHash=digest(ids),
        individualInclusion=fraction(k, n) if n else None,
        jointInclusion=fraction(k*(k-1), n*(n-1)) if n > 1 else None,
        totalEstimatorWeight=fraction(n, k) if k else None)


def sampling_design():
    return dict(version=SAMPLING, roots=list(range(1, 13)), strata=list(STRATA),
        mandatory='All three references plus the union of both arms final nominees and validation challengers; deduplicate within root',
        remaining='Partition remaining generated recipes by shared, control-only or candidate-only source membership; ignore scores',
        populationOrder='Ascending full partyId in ordinal ASCII order', sampleCount='min(2, populationCount) per root/remaining stratum',
        census='Population sizes 0, 1 or 2 consume no sampling bytes; retain every member',
        entropyBytes=ENTROPY_BYTES, word='Unsigned 16-bit little-endian, consecutive byte pairs',
        subsets='Lexicographic combinations(range(N), 2); M=N*(N-1)/2 when N>2',
        accept='word < floor(65536/M)*M; index=word % M', traversal='Roots 1..12 then declared strata order; rejected words stay consumed',
        originAssumption='One post-catalogue OS CSPRNG draw modeled as independent uniform bytes; hashes cannot prove entropy provenance',
        failure='Retain intent, available entropy and failure; no refill, redraw, replacement roots or partial plan',
        unusedBytes='Retain the entire batch and unused tail; never reuse population-sampling bytes as combat entropy', combatValuesAllocated=0)


def root_catalogue(number, reports, plans, references):
    require(len(references) == len(set(references)) == 3, 'Changed references')
    generated, parties, membership, mandatory, anchors = {}, {}, {}, set(references), {}
    for arm in ARMS:
        report, plan = reports[arm], plans[arm]
        e, scope = report['evaluation'], plan['racing']['scope']
        require(plan['version'] == report['version'] == e['version'] == RACING[arm]
                and plan['selectionPolicyVersion'] == report['selectionPolicyVersion'] == SELECTOR
                and report['planHash'] == e['planHash'] and e['status'] == 'Complete' and e['error'] is None
                and e['chargedEvaluations'] == e['plannedEvaluations'] == 528
                and len(e['panels']) == len(plan['racing']['panels']) == 6 and len(report['batches']) == 2,
                'Changed paired source contract')
        require([s['party']['id'] for s in scope['starts']] == references and scope['ownedCopies'] is None, 'Changed captured scope')
        items = [(b['wave'], i, p) for b in report['batches'] for i, p in enumerate(b['candidates'], 1)]
        generated[arm] = {p['id'] for _, _, p in items}
        require([len(b['candidates']) for b in report['batches']] == [9, 8]
                and len(generated[arm]) == 17 and not generated[arm].intersection(references), 'Changed proposal population')
        nominees, beam, challenger = e['nominees'], e['decisions'][-1]['beamIds'], e['validationFreeze']['challengerId']
        require(len(nominees) == len(set(nominees)) == 5 and set(references) < set(nominees)
                and len(beam) == len(set(beam)) == 4 and set(beam) <= generated[arm]
                and set(nominees)-set(references) <= set(beam) and challenger in nominees
                and challenger != e['validationFreeze']['benchmarkId'], 'Changed frozen finalists')
        mandatory.update(nominees+[challenger])
        membership[arm] = {}
        for wave, position, party in [(None, None, s['party']) for s in scope['starts']]+items:
            pid = party['id']
            require(pid not in parties or parties[pid]['builds'] == party['builds'], 'Changed shared recipe')
            parties[pid] = party
            membership[arm][pid] = dict(generated=pid in generated[arm], wave=wave, acceptedPosition=position,
                finalBeamRank=beam.index(pid)+1 if pid in beam else None,
                nomineeRank=nominees.index(pid)+1 if pid in nominees else None, validationChallenger=pid == challenger)
        anchors[arm] = copy.deepcopy(e['panels'][0]['observations'][0]['request']['scenario'])
        anchors[arm]['seeds'] = []
    require(plans['control']['racing'] == plans['candidate']['racing'], 'Changed paired racing scope')
    context = physical_context(anchors['control'])
    require(context == physical_context(anchors['candidate']), 'Changed shared physical context')
    scope = plans['control']['racing']['scope']
    allowed = {x['id']: x['family'] for x in scope['allowedEssences']}
    benchmark = next(s['party']['id'] for s in scope['starts'] if s['referenceId'] == plans['control']['racing']['benchmarkReferenceId'])
    rows = []
    ordered = references+sorted(mandatory-set(references))+sorted(set(parties)-mandatory)
    for pid in ordered:
        arms = [arm for arm in ARMS if pid in membership[arm]]
        group = 'shared' if len(arms) == 2 else arms[0]+'-only'
        stratum = 'reference' if pid in references else 'mandatory' if pid in mandatory else 'remaining-'+group
        scenario = copy.deepcopy(anchors['control'])
        for actor in scenario['party']:
            actor['build']['essenceIds'] = copy.deepcopy(parties[pid]['builds'][str(actor['partySlot'])])
        team = dict(partyId=pid, stratum=stratum, membership=group,
            provenance={arm: membership[arm].get(pid) for arm in ARMS}, scenario=scenario, seedFreeScenarioHash=digest(scenario))
        validate_recipe(team, context, allowed, scope['budget']['essenceSlots'])
        rows.append(team)
    strata = {s: stratum_design([t['partyId'] for t in rows if t['stratum'] == s]) for s in STRATA}
    return dict(root=number, sourceReport=f'study/pair-{number:02d}.json',
        sourcePlans={arm: f'search/root-{number:02d}/{arm}/racing/plan.json' for arm in ARMS},
        sourcePlanHashes={arm: reports[arm]['planHash'] for arm in ARMS}, benchmarkPartyId=benchmark,
        physicalContextHash=digest(context), strata=strata, teams=rows)


def catalogue():
    source = source_tools()
    evidence, publication = source.Evidence(source.RUN, source.RUN_PIN), source.Evidence(source.PUBLICATION, source.PUBLICATION_PIN)
    review, result = read(REVIEW/'review.json'), evidence.read('result.json')
    receipt = publication.read('verification.json')
    require(receipt['status'] == 'VerifiedPublishedArchiveAndCompleteLiveHistory'
            and receipt['scientificManifestSha256'] == source.RUN_PIN and receipt['scientificCloseoutSha256'] == source.CLOSEOUT_PIN
            and receipt['admissionManifestSha256'] == source.ADMISSION_PIN and sha((source.RUN/'closeout.json').read_bytes()) == source.CLOSEOUT_PIN
            and result == evidence.read('independent-audit.json')['result'] and result['version'] == SOURCE_VERSION
            and result['status'] == 'Verified' and result['decision'] == review['originalDecision'] == 'NoObservedOutputDifferentiation'
            and result['fights'] == review['sourceFights'] == 15744, 'Changed completed source study')
    context, design = evidence.read('source/context.json'), evidence.read('source/plan.json')
    source.audit.validate_policies(design)
    scope = context['scope']
    references = [s['party']['id'] for s in scope['starts']]
    require(design['roots'] == len(result['roots']) == len(review['pairs']) == 12 and scope['requiredPartySize'] == 10, 'Changed cohort')
    roots = []
    for number, (endpoint, reviewed) in enumerate(zip(result['roots'], review['pairs']), 1):
        pair = evidence.read(f'study/pair-{number:02d}.json')
        plans = {arm: evidence.read(f'search/root-{number:02d}/{arm}/racing/plan.json') for arm in ARMS}
        require(number == endpoint['root'] == reviewed['root'] and pair['status'] == 'Complete'
                and pair['planHash'] == result['planHash'] and reviewed['heldout'] == endpoint, 'Changed reviewed root')
        for arm in ARMS:
            e = pair[arm]['evaluation']
            require(plans[arm]['policy'] == design[arm] and pair[arm]['policyHash'] == digest(design[arm])
                    and plans[arm]['racing']['mechanics'] == context['mechanics']
                    and plans[arm]['damageAffinityInventory'] == context['damageAffinityInventory']
                    and [t['id'] for t in reviewed[arm]['nominees']] == e['nominees']
                    and reviewed[arm]['challenger'] == e['validationFreeze']['challengerId']
                    and reviewed[arm]['beamIds'] == e['decisions'][-1]['beamIds'], 'Changed reviewed arm')
        root = root_catalogue(number, {arm: pair[arm] for arm in ARMS}, plans, references)
        for arm in ARMS:
            require({t['partyId'] for t in root['teams'] if t['provenance'][arm] and t['provenance'][arm]['generated']}
                    == {r['id'] for r in reviewed[arm]['records']}, 'Changed reviewed population')
        roots.append(root)
    require(sum(len(r['teams']) for r in roots) == 321, 'Changed union population')
    reviewed_files = review['sources'][str(source.RUN)]
    require(all(reviewed_files.get(name) == pin for name, pin in evidence.consumed.items()), 'Source differs from reviewed evidence')
    evidence.recheck(); publication.recheck(); authenticated(REVIEW, REVIEW_PIN)
    return dict(version=VERSION, status='PopulationFrozenBeforeSampling', runnableRequest=False,
        membership='All 12 roots, both complete generated pools and references. Certainty inclusion from frozen finalist identities; scores and gate outcomes do not choose membership.',
        sources=dict(scientificManifestSha256=source.RUN_PIN, scientificCloseoutSha256=source.CLOSEOUT_PIN,
            stageReviewManifestSha256=REVIEW_PIN, publicationManifestSha256=source.PUBLICATION_PIN,
            admissionManifestSha256=source.ADMISSION_PIN, helperManifestSha256=HELPER_PIN, helperSourceSha256=HELPER_SOURCE_PIN,
            consumedScientificFiles=evidence.consumed, consumedPublicationFiles=publication.consumed),
        sourceContract=dict(studyVersion=SOURCE_VERSION, racingVersions=RACING, selectionPolicyVersion=SELECTOR,
            generatorPolicies={arm: design[arm] for arm in ARMS}, damageAffinityInventoryHash=digest(context['damageAffinityInventory']),
            scope='Frozen paired proposal union; no search replay, gate fitting, policy promotion or future-root inference'),
        capturedRuntime=dict(contentHashes=scope['contentHashes'], settingsHash=scope['settingsHash'], executionHash=scope['executionHash']),
        sampling=sampling_design(), samplesPerTeam=SAMPLES, roots=roots)


def publish_catalogue(output):
    require(not output.exists(), 'Catalogue output already exists')
    started, value = time.monotonic(), catalogue()
    output.mkdir(parents=True)
    save(output/'catalogue.json', value)
    (output/'builder.py').write_bytes(Path(__file__).read_bytes())
    save(output/'completion.json', dict(status='CatalogueFrozen', seconds=time.monotonic()-started, newFights=0, newCombatValues=0))
    return dict(status='CatalogueFrozen', manifestSha256=seal(output), catalogueSha256=sha((output/'catalogue.json').read_bytes()), roots=12, recipes=321)


def load_catalogue(package, pin):
    authenticated(package, pin)
    value = read(package/'catalogue.json')
    require(value == catalogue(), 'Catalogue does not reproduce from frozen sources')
    require(set(read(package/'files.json')) == {'catalogue.json', 'builder.py', 'completion.json'}
            and (package/'builder.py').read_bytes() == Path(__file__).read_bytes(), 'Changed catalogue producer or shape')
    return value, sha((package/'catalogue.json').read_bytes())


def choose_subset(population, entropy, cursor):
    n, k = len(population), min(2, len(population))
    require(population == sorted(set(population)) and n <= 34, 'Invalid sampling population')
    if n <= 2:
        return dict(subsetIndex=0, partyIds=list(population), rejectedWordByteOffsets=[], acceptedWordByteOffset=None), cursor
    subsets = list(combinations(range(n), k))
    m = len(subsets)
    limit, rejected = (65536//m)*m, []
    while cursor+2 <= len(entropy):
        value = int.from_bytes(entropy[cursor:cursor+2], 'little')
        offset, cursor = cursor, cursor+2
        if value < limit:
            index = value % m
            return dict(subsetIndex=index, partyIds=[population[i] for i in subsets[index]],
                        rejectedWordByteOffsets=rejected, acceptedWordByteOffset=offset), cursor
        rejected.append(offset)
    raise ValueError('Sampling batch exhausted; no refill or partial plan')


def choices(value, catalogue_hash, entropy):
    require(value['version'] == VERSION and value['sampling'] == sampling_design() and len(entropy) == ENTROPY_BYTES, 'Changed sampling design or entropy batch')
    cursor, roots = 0, []
    for root in value['roots']:
        require(root['root'] == len(roots)+1, 'Reordered sampling roots')
        strata = []
        for stratum in STRATA:
            population = [t['partyId'] for t in root['teams'] if t['stratum'] == stratum]
            design = stratum_design(population)
            require(root['strata'][stratum] == design, 'Changed sampling stratum')
            selected, cursor = choose_subset(population, entropy, cursor)
            strata.append(dict(stratum=stratum, **design, **selected))
        roots.append(dict(root=root['root'], strata=strata))
    require(len(roots) == 12, 'Incomplete sampled roots')
    return dict(version=SAMPLING, catalogueSha256=catalogue_hash, entropySha256=sha(entropy),
        roots=roots, consumedBytes=cursor, unusedBytes=len(entropy)-cursor)


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
        save(output/'choices.json', choices(value, catalogue_hash, entropy))
        save(output/'completion.json', dict(status='SamplingComplete', newFights=0, newCombatValues=0, entropyBytes=ENTROPY_BYTES, draws=1))
    except Exception as error:
        save(output/'failure.json', dict(status='SamplingFailedNoPlan', error=str(error), noRetry=True))
        raise
    return dict(status='SamplingComplete', manifestSha256=seal(output), entropyBytes=ENTROPY_BYTES, newCombatValues=0)


def verify_sampling(value, catalogue_hash, catalogue_pin, package, pin):
    manifest = authenticated(package, pin)
    require(set(manifest) == {'intent.json', 'sampler.py', 'entropy.bin', 'choices.json', 'completion.json'}, 'Incomplete or failed sampling package')
    intent, completion = read(package/'intent.json'), read(package/'completion.json')
    require(intent['version'] == SAMPLING and intent['catalogueSha256'] == catalogue_hash
            and intent['catalogueManifestSha256'] == catalogue_pin and intent['draws'] == 1 and intent['entropyBytes'] == ENTROPY_BYTES
            and intent['origin'] == 'OperatingSystemCSPRNG' and intent['newCombatValues'] == 0
            and intent['catalogueSourceSha256'] == intent['samplerSourceSha256'] == sha((package/'sampler.py').read_bytes()) == sha(Path(__file__).read_bytes())
            and completion == dict(status='SamplingComplete', newFights=0, newCombatValues=0, entropyBytes=ENTROPY_BYTES, draws=1), 'Changed sampling provenance')
    expected = choices(value, catalogue_hash, (package/'entropy.bin').read_bytes())
    require(read(package/'choices.json') == expected, 'Supplied choice differs from recorded uniform draw')
    return expected


def root_selection(population, choice):
    require(population['root'] == choice['root'] and len(choice['strata']) == len(STRATA), 'Changed sampling root')
    selected = set()
    for stratum, row in zip(STRATA, choice['strata']):
        ids = [t['partyId'] for t in population['teams'] if t['stratum'] == stratum]
        require(row['stratum'] == stratum and all(row[k] == v for k, v in stratum_design(ids).items()), 'Changed stratum probability')
        subsets = list(combinations(range(len(ids)), min(2, len(ids))))
        index = row['subsetIndex']
        require(type(index) is int and 0 <= index < len(subsets) and row['partyIds'] == [ids[i] for i in subsets[index]], 'Changed subset identity')
        selected.update(row['partyIds'])
    teams, unmeasured = [], []
    for item in population['teams']:
        certainty = item['stratum'] in ('reference', 'mandatory')
        probability = fraction(1, 1) if certainty else population['strata'][item['stratum']]['individualInclusion']
        if certainty or item['partyId'] in selected:
            teams.append(dict(copy.deepcopy(item), inclusionProbability=probability))
        else:
            unmeasured.append(dict(partyId=item['partyId'], stratum=item['stratum'], membership=item['membership'],
                provenance=copy.deepcopy(item['provenance']), inclusionProbability=probability, independentOutcome=None))
    require(len(teams) == len({t['partyId'] for t in teams}) <= 13 and len(teams)+len(unmeasured) == len(population['teams']), 'Changed measured cells')
    return dict(root=population['root'], benchmarkPartyId=population['benchmarkPartyId'], physicalContextHash=population['physicalContextHash'],
                strata=population['strata'], teams=teams, unmeasured=unmeasured)


def build_plan(value, selection, catalogue_hash, sampling_pin):
    require(value['version'] == VERSION and value['sampling'] == sampling_design() and selection['version'] == SAMPLING
            and selection['catalogueSha256'] == catalogue_hash and len(value['roots']) == len(selection['roots']) == 12, 'Changed catalogue or sample binding')
    roots = []
    for population, selected in zip(value['roots'], selection['roots']):
        require(population['root'] == selected['root'] == len(roots)+1, 'Reordered roots')
        roots.append(root_selection(population, selected))
    rates = sum(len(r['teams']) for r in roots)
    contrasts = 3*(rates-36)
    return dict(version=VERSION, status='FrozenDesignNativeIntegrationRequired', runnableRequest=False,
        catalogueSha256=catalogue_hash, samplingManifestSha256=sampling_pin, sources=value['sources'],
        sourceContract=value['sourceContract'], capturedRuntime=value['capturedRuntime'], roots=roots,
        samplesPerTeam=SAMPLES, requiredFreshCombatValues=12*SAMPLES, maximumAttemptedFights=rates*SAMPLES,
        allocation=dict(order='Root, frozen team order, common seed order', withinRoot='One fresh 256-value common panel for every measured physical recipe, deduplicated across arms',
            acrossRoots='Twelve disjoint fresh panels excluded from complete live history; repeated recipes retain separate root occurrences',
            samplingRandomness='Population-sampling bytes are never combat entropy'),
        reporting=dict(rates=rates, candidateReferenceContrasts=contrasts, approximateWilsonFamily=rates+2*contrasts,
            intervalFamily='All measured recipe rates plus gain/loss rates for every generated-recipe/reference paired contrast; established approximate family-adjusted Wilson arithmetic',
            benchmark='Fixed source benchmark; never the panel-wise best reference',
            perRootArmMean='Sum y_i/pi_i over sampled generated members of this arm, divided by 17; y_i is paired gain versus benchmark; mandatory pi=1',
            pairedPoolDifference='Per-root preserving mean minus original mean, then equal mean of 12 roots; shared members cancel using identical observations and inclusion weights',
            samplingVariance='For any declared linear total use sum_h N_h^2*(1-k_h/N_h)*s_h^2(z)/k_h over noncensus strata; z_i includes the estimand coefficient. Census strata contribute zero. Estimate conditional on fixed full-panel recipe outcomes.',
            combatUncertainty='Retain per-seed paired outcomes; estimate conditional on the selected sample using shared-seed covariance for each weighted linear contrast. Sampling and combat uncertainty are separate; do not naively add their variance estimates.',
            coverage='Report every root and stratum, both frozen nominee means and validation challengers, measured near misses/pruned candidates with provenance, all unsampled nulls and inclusion probabilities',
            limitations='No unweighted sampled-pool mean, full-pool maximum imputation, selector replay, gate fitting, independent-arm error bars for shared cells, policy promotion or future-root reliability claim',
            interpretation='DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion', decision='CompleteDiagnosticOnly'),
        requiredBeforeExecution=['A separately versioned native execution and independent-audit profile for this exact variable-size paired-pool plan; retain legacy plan/profile rejection',
            'Captured-runtime compatibility, full live-history binding and fresh admitted request',
            'Explicit cumulative time/storage allowance for admission, owned execution, both audits and publication',
            'One post-freeze combat entropy batch with every exposed fresh value permanently reserved; no retries or replacement roots',
            'Native reconstruction and independent battle/stratum audit for the exact declared fight count'],
        policyDefaultsChanged=False, newFights=0, newCombatValues=0)


def publish_plan(catalogue_package, catalogue_pin, sampling_package, sampling_pin, output):
    require(not output.exists(), 'Plan output already exists')
    value, catalogue_hash = load_catalogue(catalogue_package, catalogue_pin)
    selection = verify_sampling(value, catalogue_hash, catalogue_pin, sampling_package, sampling_pin)
    plan = build_plan(value, selection, catalogue_hash, sampling_pin)
    authenticated(catalogue_package, catalogue_pin); authenticated(sampling_package, sampling_pin)
    output.mkdir(parents=True)
    save(output/'plan.json', plan)
    save(output/'bindings.json', dict(cataloguePackage=str(catalogue_package.resolve()), catalogueManifestSha256=catalogue_pin,
        samplingPackage=str(sampling_package.resolve()), samplingManifestSha256=sampling_pin))
    (output/'builder.py').write_bytes(Path(__file__).read_bytes())
    return dict(status=plan['status'], manifestSha256=seal(output), planSha256=sha((output/'plan.json').read_bytes()),
        measuredRecipes=plan['reporting']['rates'], unmeasuredRecipes=sum(len(r['unmeasured']) for r in plan['roots']),
        futureAttemptedFights=plan['maximumAttemptedFights'], futureCombatValues=plan['requiredFreshCombatValues'])


def verify_plan(package, pin):
    manifest = authenticated(package, pin)
    require(set(manifest) == {'plan.json', 'bindings.json', 'builder.py'}, 'Changed plan package shape')
    binding = read(package/'bindings.json')
    c, s = Path(binding['cataloguePackage']), Path(binding['samplingPackage'])
    value, catalogue_hash = load_catalogue(c, binding['catalogueManifestSha256'])
    selection = verify_sampling(value, catalogue_hash, binding['catalogueManifestSha256'], s, binding['samplingManifestSha256'])
    require((package/'builder.py').read_bytes() == Path(__file__).read_bytes()
            and read(package/'plan.json') == build_plan(value, selection, catalogue_hash, binding['samplingManifestSha256']), 'Plan does not reproduce')
    authenticated(c, binding['catalogueManifestSha256']); authenticated(s, binding['samplingManifestSha256']); authenticated(package, pin)
    return dict(status='VerifiedAffinityPreservationRecognitionPlan', newFights=0, newCombatValues=0)


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
