"""Read retained receipts/recipes against unchanged policy; never run an experiment.

Intervals are reoriented from the published family-ten diagnostic. This reader
does not apply the practical family-seven assessor as a retrospective endpoint,
reconstruct combat, acquire entropy, allocate values or promote a team.
"""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RUN = ROOT/'TestResults/balance/tower-practical-selection-diagnostic-20260917'
PACKAGE = ROOT/'TestResults/selection-diagnostic-admission-20260917'
PINS = {
    'Balance Harness/Tower-Balance-Acceptance-Policy.md': '9d2bc97e660224513ffd7436fdd167ce01c3291b5f0a703f05ad175efb44810f',
    'Balance Harness/Tower-Practical-Selection-Diagnostic-Contract.md': '9e91779cca1ba485eecfddc1da4460776dd94025a7734e34b47920681c4e3643',
    'LL/tools/BalanceHarness/TowerPracticalSearch.cs': '5c480233e5ca667d5dc1374b32f8b4c5481cbcab61bae1978b4caa949d1acbec',
    'LL/tools/BalanceHarness/TowerSelectionDiagnostic.cs': '705a97e068f4fdb4166c8ea265e947f311a056a33d0baf4101332b2863b488ab',
    'Balance Harness/analysis/practical-selection-execution.py': '4b212a86e09482640633d08811ac3f05eca199cd3d1285328a6f7e50854b4de8',
    'Balance Harness/Tower-Practical-Selection-Diagnostic-Execution.json': 'a475e337dddfc6d95b22e76b72d9d28b25db3e62278671a47b0c7e371fdc777d',
    'TestResults/balance/tower-practical-selection-diagnostic-20260917/files.json': 'b73a6fc344214693dddfc14d74c4114ab4782d0ae80456ebd019c1103f149552',
    'TestResults/selection-diagnostic-execution-20260917/files.json': 'a14c73e7d330d284d1072fd62424843be4c8bd7a70c43782bd6fc70d54d824df',
}


def require(ok, message):
    if not ok:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()


def review():
    for name, digest in PINS.items():
        require(sha(ROOT/name) == digest, 'Changed policy/source/evidence: '+name)
    spec = importlib.util.spec_from_file_location('selection_receipts', ROOT/'Balance Harness/analysis/practical-selection-execution.py')
    receipts = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(receipts)
    checked = receipts.verify()
    require(checked == read(ROOT/'Balance Harness/Tower-Practical-Selection-Diagnostic-Execution.json'), 'Execution review differs.')
    require(checked['receipt']['status'] == 'CompletedVerified', 'Diagnostic did not complete.')
    # Authenticate the frozen package and confirm this review applies to unchanged
    # live producing source/content, without calling the native verifier again.
    package_files = receipts.inventory(PACKAGE)
    manifest = read(PACKAGE/'files.json')
    require(set(package_files) == set(manifest) | {'files.json', 'preparation-receipt.json'}, 'Admission membership differs.')
    require(all(package_files[n] == h for n, h in manifest.items()), 'Admission bytes differ.')
    admission = read(receipts.OUTPUT/'admission-revalidation.json')
    require(package_files['files.json'] == admission['packageManifestSha256']
            and package_files['preparation-receipt.json'] == admission['packageReceiptSha256'], 'Admission seal differs.')
    for name, digest in read(PACKAGE/'source-files.json').items():
        require(sha(ROOT/name) == digest, 'Producing source changed: '+name)
    definition = read(RUN/'source-definition.json')
    request = read(RUN/'request.json')
    for name, digest in definition['contentHashes'].items():
        require(sha(Path(request['operation']['contentRoot'])/'Data'/name) == digest, 'Live content changed: '+name)
    result, teams = read(RUN/'result.json'), read(RUN/'teams.json')
    study = read(RUN/'study/study.json')
    frozen = read(RUN/'study/nominees-freeze.json')
    primary = next(t for t in teams['teams'] if t['primary'])
    require(primary['partyId'] == result['primaryId'] == frozen['primaryId']
            and frozen == study['freeze'] and frozen['completedAttempts'] == 640,
            'Frozen primary identity differs.')
    require(teams['adoption'] == 'Hold' and not primary['recommended'] and not primary['referenceIds']
            and result['diagnosticDecision'] == 'NoSelectionMissDemonstrated'
            and result['balanceAssessment'] == 'NotAssessed', 'Published scope/decision differs.')
    require(all(not t['scenario']['seeds'] for t in teams['teams']), 'Export includes execution schedules.')
    primary_rate = next(r for r in result['rates'] if r['partyId'] == primary['partyId'])
    anchors, comparisons = [], []
    for team in teams['teams']:
        if not team['referenceIds']:
            continue
        require(team['recommended'], 'Anchor recommendation changed.')
        anchors.append(team)
        rate = next(r for r in result['rates'] if r['partyId'] == team['partyId'])
        contrast = next(c for c in result['contrasts'] if c['otherPartyId'] == team['partyId'])
        # An algebraic reversal of the existing reported quantity and interval,
        # with no narrower family, new test, additional endpoint or promotion.
        require(primary_rate['wins']-rate['wins'] == contrast['losses']-contrast['gains'], 'Paired difference mismatch.')
        comparisons.append({'referenceId': team['referenceIds'][0], 'anchorPartyId': team['partyId'],
            'anchorWins': rate['wins'], 'primaryOnlyWins': contrast['losses'], 'anchorOnlyWins': contrast['gains'],
            'primaryMinusAnchor': -contrast['observedGain'],
            'retainedFamilyTenLower': -contrast['upper'], 'retainedFamilyTenUpper': -contrast['lower']})
    require(len(anchors) == 2, 'Expected both original anchors.')
    anchor = next(t for t in anchors if t['referenceIds'] == ['team-040e60d3dbc5c127321653c47ed3a9d3'])
    changes, unchanged = [], 0
    for candidate_slot, reference_slot in zip(primary['scenario']['party'], anchor['scenario']['party'], strict=True):
        candidate, reference = copy.deepcopy(candidate_slot), copy.deepcopy(reference_slot)
        ca, ra = set(candidate['build'].pop('essenceIds')), set(reference['build'].pop('essenceIds'))
        require(candidate == reference and len(ca) == len(ra) == 5, 'Non-Essence cohort change.')
        unchanged += len(ca & ra)
        if ca != ra:
            changes.append({'partySlot': candidate['partySlot'], 'removed': sorted(ra-ca), 'added': sorted(ca-ra)})
    require(unchanged == 48 and len(changes) == 1 and changes[0]['partySlot'] == 6, 'Primary recipe delta differs.')
    provenance = [p['provenance'] for a in study['discovery']['arms'] for p in a['proposals']
                  if p.get('party', {}).get('id') == primary['partyId'] and p.get('result') == 'evaluated']
    require(len(provenance) == 1, 'Ambiguous primary provenance.')
    selection = [{'partyId': n['id'], 'wins': sum(n['cells'][0]['clears']), 'samples': len(n['cells'][0]['clears'])}
                 for n in study['selection']]
    ledger = read(RUN/'seed-ledger.json')
    require(ledger['reservationState'] == 'Complete' and len(set(ledger['historical']+ledger['reserved'])) == 486374,
            'Reservation union differs.')
    numerical = (primary_rate['estimate']['lower'] >= .10
                 and all(c['primaryMinusAnchor'] >= .05 and c['retainedFamilyTenLower'] > 0 for c in comparisons))
    require(numerical and definition['budgetPurpose'] == 'intended-progression', 'Review assumptions differ.')
    return {
        'version': 'tower-practical-primary-adoption-readiness-v1',
        'scope': 'Read-only policy and retained-recipe review; no new scientific endpoint.',
        'decision': 'HoldNoProspectiveAdoptionEndpoint',
        'primaryId': primary['partyId'], 'primaryWins': primary_rate['wins'], 'samples': 1000,
        'primaryRetainedRateInterval': primary_rate['estimate'],
        'primaryIsNovelRecipe': True, 'primarySelectedBeforeConfirmation': True,
        'freezeCompletedAttempts': frozen['completedAttempts'], 'selection': selection,
        'anchorComparisonsReorientedOnly': comparisons,
        'observedNumericalMarginsMeetPracticalThresholdsUsingRetainedFamilyTenIntervals': numerical,
        'practicalFamilySevenEndpointApplied': False, 'practicalStrengthDecision': None,
        'recommendationChanged': False, 'adoption': teams['adoption'],
        'nominalFamilySize': 10, 'intervalCoverage': 'Approximate; unchanged published diagnostic intervals.',
        'policyBoundary': 'The frozen diagnostic expressly excludes the practical strength gate and keeps anchor recommendations and Hold.',
        'budget': definition['budget'], 'budgetPurpose': definition['budgetPurpose'], 'ownedCopies': definition['ownedCopies'],
        'recipeChangesFrom040e': changes, 'unchangedCharacterEssenceAssignments': unchanged,
        'sameNonEssencePartyFieldsAs040e': True, 'requiredCopies': primary['requiredCopies'],
        'provenance': provenance[0], 'independentDiscoveryFromScratch': False,
        'causalEffectOfIndividualReplacementsEstablished': False,
        'balancePolicyObservation': 'Above50PercentCeilingInDeclaredIntendedProgressionCohort',
        'allFourObservedRatesAboveCeiling': all(r['estimate']['rate'] > .5 for r in result['rates']),
        'allFourRetainedRateLowerBoundsAboveCeiling': all(r['estimate']['lower'] > .5 for r in result['rates']),
        'publishedBalanceAssessment': result['balanceAssessment'], 'formalBalanceAssessmentRecomputed': False,
        'searchMethodReliability': 'Unresolved', 'v19RequiredRecipes': 253, 'v19UnusedValues': 512,
        'permanentExclusions': 486374, 'newFights': 0, 'newReservations': 0, 'replays': 0,
        'nativeReconstructions': 0, 'extraPublicVerifierRuns': 0,
        'nextStep': 'Prepare a prospective fixed-primary versus both-anchor confirmation plan under the existing practical strength criteria; no new search or executable request is authorized here.',
        'remainingBeforeAnyRun': [
            'Choose and freeze a compatible fixed-recipe execution/audit route; the ordinary practical command performs search and is not a fixed-team confirmation command.',
            'Predeclare both contrasts, family-seven rate/discordance quantities, exact sample size, sampling model, power alternative and no-extension stopping rule.',
            'Bind unchanged exact recipes/cohort/content/runtime, full current exclusions and fresh independent panel; do not reuse the diagnostic panel or its exposed tail.',
            'Account for setup, complete-history admission, all three teams, audit/publication and closeout under a concrete new allowance; completed scopes provide none.'
        ],
        'authenticatedNativeFiles': checked['nativeInventoryEntries'], 'authenticatedOversightFiles': checked['oversightInventoryEntries']+2,
        'policyAndEvidencePins': PINS,
    }


if __name__ == '__main__':
    print(json.dumps(review(), indent=2))
