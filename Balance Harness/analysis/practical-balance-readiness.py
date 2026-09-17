"""Read saved evidence and reproduce a retrospective readiness review; no execution route.

Authenticates the completed fixed-team inventories via their read-only reader.
Older studies use selected manifest-bound receipts, not a new native reconstruction.
Writes nothing, allocates no values, and invokes no subprocess or combat API.
"""
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import re
from statistics import NormalDist
import sys

ROOT = Path(__file__).resolve().parents[2]
HERE = ROOT / 'TestResults/practical-balance-readiness-review-20260917'
RUN = 'TestResults/balance/tower-practical-fixed-team-confirmation-20260917/'
FOCUSED = 'TestResults/balance/tower-focused-challenger-run-20260915/'
PORTFOLIO = 'TestResults/balance/tower-portfolio-confirmation-execution-20260914/'
MIDPOINT = 'TestResults/balance/tower-captured-v19-midpoint-20260914/'
ADMISSION = 'TestResults/fixed-team-confirmation-admission-20260917-03/'
RESULT = ROOT / 'Balance Harness/Tower-Practical-Balance-Readiness.json'


def require(value, message):
    if not value:
        raise ValueError(message)


def read(path):
    return json.loads((ROOT / path).read_text(encoding='utf-8-sig'))


def sha(path):
    with (ROOT / path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def bound_members(base, manifest, names):
    entries = read(base + manifest)
    for name in names:
        require(entries.get(name) == sha(base + name), 'Changed receipt: ' + base + name)


def wilson(wins, trials):
    z = NormalDist().inv_cdf(1 - .05 / (2 * 7))
    p = wins / trials
    center = (p + z*z / (2*trials)) / (1 + z*z / trials)
    radius = z * math.sqrt(p*(1-p)/trials + z*z/(4*trials*trials)) / (1 + z*z/trials)
    return center - radius, center + radius


def equipment_only(party):
    return [{**row, 'build': {k: v for k, v in row['build'].items()
                             if k not in ('essenceIds', 'identityEssenceIds')}} for row in party]


def review():
    pins = read(HERE / 'inputs.json')
    for name, digest in pins.items():
        require(sha(name) == digest, 'Changed review input: ' + name)
    path = ROOT / 'Balance Harness/analysis/practical-fixed-team-confirmation-execution.py'
    spec = importlib.util.spec_from_file_location('fixed_saved_execution', path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    execution = module.verify()  # Saved hashes and receipts only; never module.run().
    require(execution == read('Balance Harness/Tower-Practical-Fixed-Team-Confirmation-Execution.json'),
            'Fixed-team review differs from authenticated publication.')
    focused_members = ['control/completion.json', 'control/audit-result.json', 'control/verify-result.json',
                       'control/execution-identity.json', 'study/source.json', 'study/assessment.json']
    bound_members(FOCUSED, 'evidence-files.json', focused_members)
    bound_members(PORTFOLIO, 'work-files.json', ['final-verification.json', 'confirmation-results.json'])
    bound_members(MIDPOINT, 'midpoint-files.json', ['result.json'])
    retained_root = 'TestResults/balance/tower-retained-family-audit-20260914/native/'
    bound_members(retained_root, 'files.json', ['result.json'])
    retained = read(retained_root + 'result.json')
    bound_members(ADMISSION, 'files.json', ['settings-and-execution.json', 'source-files.json'])
    source_pins = read(ADMISSION + 'source-files.json')
    limits_path = 'LL/tools/BalanceHarness/TowerStudyLimits.cs'
    require(source_pins[limits_path] == sha(limits_path), 'History limit differs from admitted source.')
    limit = int(re.search(r'HistoricalSeeds\s*=\s*([\d_]+)',
                         (ROOT / limits_path).read_text()).group(1).replace('_', ''))
    result = execution['observation']['result']
    definition = read(RUN + 'source-definition.json')
    study = read(RUN + 'study/study.json')
    require(study['freeze']['definition'] == definition, 'Definition mismatch.')
    trials_by_id = {row['partyId']: row['trials'] for row in study['evidence']}
    rates = []
    for row in result['rates']:
        trials = trials_by_id[row['partyId']]
        require(len(trials) == 5500 and len({t['seed'] for t in trials}) == 5500, 'Incomplete panel.')
        require(all(t['outcome'] in ('Victory', 'Defeat', 'Draw') for t in trials), 'Invalid outcome.')
        wins = sum(t['outcome'] == 'Victory' for t in trials)
        require(wins == row['wins'], 'Win count mismatch.')
        lower, upper = wilson(wins, len(trials))
        require(abs(lower-row['estimate']['lower']) < 1e-8 and
                abs(upper-row['estimate']['upper']) < 1e-8, 'Wilson mismatch.')
        rates.append({'partyId': row['partyId'], 'wins': wins, 'trials': len(trials),
                      'rate': wins/len(trials), 'adjustedLower': lower, 'adjustedUpper': upper,
                      'lowerAboveCeilingPercentagePoints': 100*(lower-.5)})
    candidate = trials_by_id[result['candidateId']]
    for contrast in result['contrasts']:
        other = trials_by_id[contrast['otherPartyId']]
        require([t['seed'] for t in candidate] == [t['seed'] for t in other], 'Unpaired evidence.')
        gains = sum(a['outcome'] == 'Victory' and b['outcome'] != 'Victory' for a,b in zip(candidate, other))
        losses = sum(a['outcome'] != 'Victory' and b['outcome'] == 'Victory' for a,b in zip(candidate, other))
        require((gains, losses) == (contrast['gains'], contrast['losses']), 'Contrast mismatch.')
        require(abs(wilson(gains, 5500)[0]-wilson(losses, 5500)[1]-contrast['lower']) < 1e-8,
                'Paired lower mismatch.')
    diagnostic = read('TestResults/balance/tower-practical-selection-diagnostic-20260917/source-definition.json')
    require(diagnostic['budgetPurpose'] == 'intended-progression' and diagnostic['ownedCopies'] is None,
            'Changed declared cohort.')
    require(diagnostic['contentHashes'] == definition['contentHashes'] and
            diagnostic['settingsHash'] == definition['settingsHash'], 'Diagnostic cohort content mismatch.')
    templates = diagnostic['contexts'][0]['characterTemplates']
    for team in definition['teams']:
        scenario = team['scenario']
        require(equipment_only(scenario['party']) == equipment_only(templates), 'Equipment mismatch.')
        require(scenario['floorNumber'] == 5 and len(scenario['party']) == 10, 'Cohort mismatch.')
        require(all(len(r['build']['essenceIds']) == 5 and
                    r['build']['essenceIds'] == sorted(r['build']['essenceIds']) for r in scenario['party']),
                'Slots or canonical ordering changed.')
    focused_source = read(FOCUSED + 'study/source.json')
    focused_audit = read(FOCUSED + 'control/audit-result.json')
    focused_completion = read(FOCUSED + 'control/completion.json')
    focused_assessment = read(FOCUSED + 'study/assessment.json')
    require(focused_completion['focusedOutcome'] == focused_assessment['outcome'] == 'Pass' and
            focused_audit['cells'] == len(focused_assessment['cells']) == 989 and
            not focused_completion['fullRetainedFamilyPass'], 'Changed focused scope.')
    require(read(FOCUSED + 'control/verify-result.json')['exitCode'] == 0, 'Saved native audit failed.')
    old_anchor = focused_source['scenarios'][focused_audit['bestCell']]['party']
    current_anchor = definition['teams'][1]['scenario']['party']
    require(equipment_only(old_anchor) == equipment_only(current_anchor), 'Older reference equipment differs.')
    same_sets = all(set(a['build']['essenceIds']) == set(b['build']['essenceIds'])
                    for a,b in zip(old_anchor, current_anchor))
    changed_order_slots = [a['partySlot'] for a,b in zip(old_anchor, current_anchor)
                           if a['build']['essenceIds'] != b['build']['essenceIds']]
    current_identity = read(ADMISSION + 'settings-and-execution.json')['execution']
    old_identity = read(FOCUSED + 'control/execution-identity.json')
    changed_assemblies = [k for k,v in current_identity['assemblyHashes'].items()
                          if v != old_identity['assemblyHashes'].get(k)]
    changed_content = [k for k,v in definition['contentHashes'].items()
                       if v != focused_source['contentHashes'].get(k)]
    scalars = {}
    for label, content_root, hashes in [
        ('current', ROOT/'LL/src/API/API.LL', definition['contentHashes']),
        ('capturedPlus10', Path(focused_source['contentRoot']), focused_source['contentHashes'])]:
        path = content_root/'Data/world-tower/tower-floors.json'
        require(sha(path) == hashes['world-tower/tower-floors.json'], 'Changed floor content.')
        floor = next(x for x in read(path)['floors'] if x['floorNumber'] == 5)
        scalars[label] = floor['guardianScaling']
    portfolio = read(PORTFOLIO + 'final-verification.json')
    counts = read(PORTFOLIO + 'confirmation-results.json')
    for key in ('familyRecipes', 'passedRestarts', 'reliability', 'ordinary', 'joint'):
        require(portfolio[key] == counts['recipes' if key == 'familyRecipes' else key], 'Portfolio receipt mismatch.')
    original = read('TestResults/balance/tower-search-portfolio-work-20260914/final-verification.json')
    midpoint = read(MIDPOINT + 'result.json')
    reservations = execution['observation']['reservation']['cumulativeValues']
    unused_tail = execution['observation']['confirmation-binding']['newReservations'] - len(candidate)
    return {
        'version': 'tower-practical-balance-readiness-v1',
        'status': 'SavedEvidenceReviewComplete',
        'formalFixedTeamAdoption': result['adoption'],
        'formalFixedTeamBalanceAssessment': result['balanceAssessment'],
        'retrospectiveBalanceImplication': 'SupportedCeilingBreachesInDeclaredCohort',
        'rates': rates,
        'cohort': {'purpose': diagnostic['budgetPurpose'], 'budget': diagnostic['budget'],
                   'partySize': 10, 'ownedCopies': None, 'canonicalOrderVerified': True,
                   'fixedEquipmentMatchesDeclaredTemplates': True},
        'capturedComparison': {'settingsEqual': definition['settingsHash'] == focused_source['settingsHash'],
            'equipmentEqualFor040e': True, 'essenceSetsEqualFor040e': same_sets,
            'changedOrderSlotsFor040e': changed_order_slots, 'changedContentFiles': changed_content,
            'changedAssemblies': changed_assemblies, 'guardianScaling': scalars,
            'transferOfBalancePassSupported': False},
        'historicalObligations': {
            'originalV19': {'status': original['status'], 'reliability': original['reliability'],
                'ordinary': original['ordinaryFamilyAssessment'], 'joint': original['jointFamilyAssessment'],
                'requiredRecipes': portfolio['familyRecipes'], 'unusedValues': portfolio['unusedV19ConfirmationSeeds']},
            'separateCapturedConfirmation': {k: portfolio[k] for k in ('familyRecipes', 'passedRestarts',
                'reliability', 'adoption', 'ordinary', 'joint', 'observedAboveCeiling', 'jointSupportedAboveCeiling')},
            'midpoint': {'status': midpoint['status'], 'factor': midpoint['factor'], 'recipes': len(midpoint['cells'])},
            'focused': {k: focused_audit[k] for k in ('focusedOutcome', 'fullRetainedFamilyPass', 'cells',
                'trials', 'bestWins', 'bestInterval', 'viableLowerBounds', 'ceilingUpperBoundsAboveHalf')},
            'retainedFamily': {'entries': retained['entries'],
                'outsideFocusedConfirmation': retained['entries']-focused_audit['cells']},
            'currentMethodReliability': 'NotEstablishedByFixedTeamConfirmation'},
        'history': {'reserved': reservations, 'sourcePinnedLimit': limit, 'headroom': limit-reservations,
            'fixedTeamMaximumBatchWords': 11000, 'fixedTeamHistoryAdmissionMaximum': limit-11000,
            'fitsHistoryLimitOnly': reservations <= limit-11000, 'newRunAuthorized': False,
            'freshRegistryScanPerformed': False, 'unusedExposedTail': unused_tail},
        'nextStep': 'Seed-free current-gameplay calibration scope and complete recipe/context coverage inventory',
        'scope': {'newFights': 0, 'newSeedValues': 0, 'nativeCommands': 0, 'newTestsOrBuilds': 0,
                  'tuning': False, 'historicalResultsRewritten': False, 'migrations': False, 'deployment': False},
        'authentication': {'fixedTeam': 'Complete saved native and oversight inventories',
            'olderStudies': 'Selected manifest-bound receipts only; no new full-archive reconstruction',
            'pinnedInputs': len(pins)},
    }


if __name__ == '__main__':
    require(len(sys.argv) == 1 or sys.argv[1:] == ['verify'], 'Only read-only output or verify is supported.')
    value = review()
    if sys.argv[1:] == ['verify']:
        require(value == read(RESULT), 'Saved review differs from reproduced findings.')
        print(json.dumps({'status': 'Passed', 'pinnedInputs': value['authentication']['pinnedInputs'],
                          'fixedTeamAdoption': value['formalFixedTeamAdoption'],
                          'retrospectiveBalanceImplication': value['retrospectiveBalanceImplication'],
                          'historyHeadroom': value['history']['headroom']}))
    else:
        print(json.dumps(value, indent=2, allow_nan=False))
