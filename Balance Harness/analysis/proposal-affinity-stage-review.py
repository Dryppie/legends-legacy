"""Read-only diagnosis of the closed proposal-affinity pilot 02.

Authenticates consumed evidence, recounts all saved racing stages, and traces
changed accepted positions. No combat, entropy, policy fitting or promotion.
The prior publication verifier owns the full battle and reservation-history audit.
"""
import argparse
from collections import Counter
import importlib.util
import json
import os
from pathlib import Path
import threading
import time

ROOT = Path(__file__).resolve().parents[2]
HELPER = Path(__file__).with_name('adaptive-racing-stage-review.py')
SPEC = importlib.util.spec_from_file_location('affinity_stage_kernel', HELPER)
kernel = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(kernel)
require, sha = kernel.require, kernel.sha
RUN = ROOT/'TestResults/balance/tower-proposal-affinity-pilot-02-20260923'
RUN_PIN = 'f0d4d22531f4c46426c28224102d193a91c0d20318377b71b95e2fe19f4acc33'
CLOSEOUT_PIN = '4211f73e7c23a23819d913d0ef7e3e59c7b0b0060984765d7a1b89b6cebf744e'
ADMISSION = ROOT/'TestResults/proposal-affinity-pilot-02-admission-repaired-20260923'
ADMISSION_PIN = 'e5172f3b6b3db3ddcbe2e4a68cdd96eeb04e6822a3e65d12078676b1da7979fa'
PUBLICATION = ROOT/'TestResults/proposal-affinity-pilot-02-publication-verification-20260923'
PUBLICATION_PIN = '8d5d09d1a690862390afa87d3f5fa6b661c46505ee6f75a8311573c646ba32dd'
OUT = ROOT/'TestResults/proposal-affinity-stage-review-20260923'
VERSION = 'tower-proposal-racing-v2'
SECONDS, BYTES = 180, 64*1048576


class Evidence(kernel.Evidence):
    def bytes(self, name):
        require(name in self.manifest and not Path(name).is_absolute()
                and '..' not in Path(name).parts, 'Unbound evidence path')
        raw = (self.root/name).read_bytes()
        require(sha(raw) == self.manifest[name], 'Changed consumed evidence: '+name)
        self.consumed[name] = self.manifest[name]
        return raw


def digest(value):
    return sha(json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=True).encode())


def active_affinities(party, affinities, selected):
    by_id = {a['id']: a for a in affinities}
    require(len(by_id) == len(affinities) and len(selected) == len(set(selected))
            and set(selected) <= by_id.keys(), 'Unresolved or duplicate affinity')
    rows, protected = [], {}
    for owner, build in party['builds'].items():
        for aid in selected:
            a = by_id[aid]
            pair = {a['producerEssenceId'], a['modifierEssenceId']}
            if pair <= set(build):
                rows.append(dict(parentId=party['id'], owner=int(owner), affinityId=aid,
                    producerEssenceId=a['producerEssenceId'], modifierEssenceId=a['modifierEssenceId']))
                protected.setdefault(owner, set()).update(pair)
    return dict(routes=rows, routeCount=len(rows), uniqueEssencePairs=len({
        tuple(sorted((r['producerEssenceId'], r['modifierEssenceId']))) for r in rows}),
        protectedByOwner={o: sorted(ids) for o, ids in protected.items()},
        distinctProtectedEssences=len(set().union(*protected.values())) if protected else 0,
        activeOwners=sorted(map(int, protected)),
        removableByOwner={o: sorted(set(party['builds'][o])-ids) for o, ids in protected.items()})


def edits(parent, party):
    require(parent['builds'].keys() == party['builds'].keys(), 'Changed owner membership')
    return [dict(owner=int(o), removed=sorted(set(old)-set(party['builds'][o])),
        added=sorted(set(party['builds'][o])-set(old))) for o, old in parent['builds'].items()
        if old != party['builds'][o]]


def accepted_positions(left, right):
    """Pair accepted slots, retaining attempt identity instead of assuming no rejects."""
    a = [p for p in left if p['rejection'] is None]
    b = [p for p in right if p['rejection'] is None]
    require(len(a) == len(b), 'Unpaired accepted positions')
    return [(i, x, y, all(x[k] == y[k] for k in ('attempt', 'parents', 'scheduledOwners', 'requestedOperator')))
            for i, (x, y) in enumerate(zip(a, b), 1)]


def shared_outcomes(left, right):
    def normalized(report):
        rows = {}
        for panel in report['evaluation']['panels']:
            for row in panel['observations']:
                request, outcome = row['request'], row['outcome']
                key = (request['role'], request['partyId'], request['seed'])
                require(key not in rows, 'Duplicate shared measurement key')
                rows[key] = {k: outcome[k] for k in ('seed', 'outcome', 'guardianHealth', 'survival', 'durationSeconds')}
        return rows
    a, b = normalized(left), normalized(right)
    shared = a.keys() & b.keys()
    require(all(kernel.same(a[k], b[k]) for k in shared), 'Shared recipe outcomes disagree')
    return len(shared)


def arm_review(report, plan, context, policy, heldout, selected):
    require(plan['version'] == VERSION and plan['policy'] == policy and report['policyHash'] == digest(policy)
            and plan['damageAffinityInventory'] == context['damageAffinityInventory'], 'Changed proposal contract')
    scope = plan['racing']['scope']
    references = {s['party']['id'] for s in scope['starts']}
    primary = next(s['party']['id'] for s in scope['starts'] if s['referenceId'] == scope['stages']['selectionPrimaryReferenceId'])
    benchmark = next(s['party'] for s in scope['starts'] if s['referenceId'] == plan['racing']['benchmarkReferenceId'])
    require(references == kernel.REFERENCES and primary == kernel.PRIMARY and benchmark['id'] == kernel.BENCHMARK,
            'Saved references differ from the review kernel contract')
    allowed = {e['id']: e['family'] for e in scope['allowedEssences']}
    parties = {s['party']['id']: s['party'] for s in scope['starts']}
    for batch in report['batches']:
        for p in batch['proposals']:
            require(p['requestedOperator'] == p['effectiveOperator'] == 'single'
                    and p['parents'] == [benchmark['id']] and p['parentSource'] == 'benchmark'
                    and 0 <= p['constructionChecks'] <= 32, 'Changed single-edit lineage')
            if p['party'] is not None:
                party = p['party']; delta = edits(benchmark, party)
                require(party['id'] == digest(party['builds']) and len(delta) == 1
                        and len(delta[0]['removed']) == len(delta[0]['added']) == p['replacementDistance'] == 1
                        and p['changedOwners'] == [delta[0]['owner']], 'Changed recipe identity or edit')
                for ids in party['builds'].values():
                    require(ids == sorted(set(ids)) and len(ids) == scope['budget']['essenceSlots']
                            and all(e in allowed for e in ids) and len({allowed[e] for e in ids}) == len(ids),
                            'Illegal essence family placement')
                parties[party['id']] = party
        require(batch['feedbackPanels'] == [p['observations'][0]['request']['panelHash']
            for p in report['evaluation']['panels'][:2*(batch['wave']-1)]], 'Changed feedback binding')
    for panel in report['evaluation']['panels']:
        f = panel['freeze']
        require(f['version'] == VERSION and f['planHash'] == report['planHash']
                and all(p == parties.get(p['id']) for p in f['parties']), 'Changed panel recipe or binding')
    result = kernel.adaptive(report, plan['racing'], heldout, selected, report_version=VERSION)
    by_id = {r['id']: r for r in result['records']}
    for batch in report['batches']:
        for position, p in enumerate((p for p in batch['proposals'] if p['rejection'] is None), 1):
            by_id[p['party']['id']].update(position=position, attempt=p['attempt'],
                scheduledOwners=p['scheduledOwners'], edits=edits(benchmark, p['party']))
    result['finalBeam'] = [dict(rank=i, **next(s for s in report['evaluation']['decisions'][1]['commonScores'] if s['id'] == pid))
        for i, pid in enumerate(report['evaluation']['decisions'][1]['beamIds'], 1)]
    return result


def analyze(scientific, admission):
    result = scientific.read('result.json')
    require(scientific.read('independent-audit.json')['result'] == result
            and result['status'] == 'Verified' and result['decision'] == 'AbandonThisConfiguration', 'Changed audited result')
    context, design = scientific.read('source/context.json'), scientific.read('source/plan.json')
    preview = admission.read('preview-batches.json')
    benchmark = next(s['party'] for s in context['scope']['starts'] if s['referenceId'] == context['benchmarkReferenceId'])
    coverage = active_affinities(benchmark, preview['damageSourceAffinities']['affinities'], design['candidate']['preservedDamageAffinityIds'])
    arm = next(a for a in preview['arms'] if a['name'] == design['candidate']['name'])
    require(arm['protectedDamageAffinities'] == coverage['routes'], 'Changed active parent affinities')
    # Retain the exact producing implementation used to interpret the saved edits.
    for filename in ('TowerAdaptiveRacingGenerator.cs', 'TowerProposalPolicies.cs', 'TowerBatchRacing.cs'):
        admission.bytes('source/LL/tools/BalanceHarness/'+filename)
    pairs, changed, counts = [], [], Counter()
    require(len(result['roots']) == design['roots'] == 12 and design['heldoutSamples'] == 256, 'Changed study size')
    for endpoint in result['roots']:
        number = endpoint['root']; require(number == len(pairs)+1, 'Reordered roots')
        pair = scientific.read(f'study/pair-{number:02d}.json')
        require(pair['status'] == 'Complete' and pair['planHash'] == result['planHash'], 'Changed root binding')
        heldout = {}
        for role in ('control', 'candidate', 'benchmark'):
            pid, wins = endpoint[role+'Party'], endpoint[role+'Wins']
            require(pid not in heldout or heldout[pid] == wins, 'Shared held-out outcome disagrees')
            heldout[pid] = wins
        reviewed, plans = {}, {}
        for role in ('control', 'candidate'):
            plans[role] = scientific.read(f'search/root-{number:02d}/{role}/racing/plan.json')
            reviewed[role] = arm_review(pair[role], plans[role], context, design[role], heldout, endpoint[role+'Party'])
            for batch in pair[role]['batches']:
                for p in batch['proposals']:
                    counts[role+'Attempts'] += 1
                    counts[role+'Rejected'] += p['rejection'] is not None
                    counts[role+'ConstructionChecks'] += p['constructionChecks']
        require(plans['control']['racing'] == plans['candidate']['racing'], 'Changed paired racing plan')
        counts['sharedTrainingObservations'] += shared_outcomes(pair['control'], pair['candidate'])
        root_changes = []
        for wave, (a, b) in enumerate(zip(pair['control']['batches'], pair['candidate']['batches']), 1):
            wave_changes = 0
            for position, x, y, aligned in accepted_positions(a['proposals'], b['proposals']):
                counts['acceptedPositions'] += 1
                counts['alignedAttempts'] += aligned
                counts['activeOwnerPositions'] += any(str(o) in coverage['protectedByOwner'] for o in x['scheduledOwners'])
                if x['party']['id'] == y['party']['id']: continue
                wave_changes += 1
                item = dict(root=number, wave=wave, position=position, alignedAttempt=aligned)
                for role, proposal in [('control', x), ('candidate', y)]:
                    item[role] = next(r for r in reviewed[role]['records'] if r['id'] == proposal['party']['id'])
                changed.append(item)
            root_changes.append(wave_changes)
        require(root_changes == endpoint['changedPositionsPerWave'], 'Changed proposal divergence count')
        pairs.append(dict(root=number, **reviewed, heldout=endpoint))
    counts.update(changedPositions=len(changed), changedRoots=len({x['root'] for x in changed}),
        differingOutputs=sum(not p['heldout']['identical'] for p in pairs))
    for role in ('control', 'candidate'):
        counts[role+'ChangedNominations'] = sum(x[role]['nominated'] for x in changed)
        counts[role+'ChangedSelections'] = sum(x[role]['selected'] for x in changed)
        counts[role+'ChangedHeldoutMeasured'] = sum(x[role]['heldoutWins'] is not None for x in changed)
        counts[role+'ChangedInitialWins'] = sum(x[role]['initialWins'] for x in changed)
    root5 = pairs[4]; selected = root5['candidate']['selection']['selected']
    trace = dict(root=5, selectedCandidate=selected, heldout=root5['heldout'])
    for role in ('control', 'candidate'):
        trace[role] = dict(selectedRecipePath=next((r for r in root5[role]['records'] if r['id'] == selected), None),
            finalBeam=root5[role]['finalBeam'], selection=root5[role]['selection'])
    return dict(originalDecision=result['decision'], sourceFights=result['fights'], coverage=coverage,
        summary=dict(counts), changedPositions=changed, root5=trace, pairs=pairs)


def run(output):
    output = output.resolve()
    sources = [(RUN, RUN_PIN), (ADMISSION, ADMISSION_PIN), (PUBLICATION, PUBLICATION_PIN)]
    require(output.is_relative_to(ROOT/'TestResults') and not output.exists()
            and all(not output.is_relative_to(p) for p, _ in sources), 'Use a new external review directory')
    started = time.monotonic(); output.mkdir()
    def save(name, value):
        raw = (json.dumps(value, indent=2, allow_nan=False)+'\n').encode()
        require(sum(p.stat().st_size for p in output.iterdir())+len(raw) < BYTES, 'Review storage allowance exhausted')
        with (output/name).open('xb') as stream: stream.write(raw)
    save('declaration.json', dict(kind='ReadOnlyProposalAffinityStageReview', chargedSeconds=SECONDS, chargedBytes=BYTES,
        newFights=0, newValues=0, scope='Full separate engineering allowance charged on success or failure; no scientific extension.'))
    timer = threading.Timer(SECONDS, lambda: os._exit(124)); timer.daemon = True; timer.start()
    try:
        scientific, admission, publication = [Evidence(p, pin) for p, pin in sources]
        receipt = publication.read('verification.json')
        require(receipt['status'] == 'VerifiedPublishedArchiveAndCompleteLiveHistory'
                and receipt['scientificManifestSha256'] == RUN_PIN and receipt['scientificCloseoutSha256'] == CLOSEOUT_PIN
                and receipt['admissionManifestSha256'] == ADMISSION_PIN
                and sha((RUN/'closeout.json').read_bytes()) == CLOSEOUT_PIN, 'Changed publication binding')
        helper_raw, source_raw = HELPER.read_bytes(), Path(__file__).read_bytes()
        review = analyze(scientific, admission)
        for evidence in (scientific, admission, publication): evidence.recheck()
        require(sha((RUN/'closeout.json').read_bytes()) == CLOSEOUT_PIN and helper_raw == HELPER.read_bytes()
                and source_raw == Path(__file__).read_bytes(), 'Review inputs changed while running')
        review.update(version='tower-proposal-affinity-stage-review-v1', newFights=0, newValues=0,
            interpretation='RetrospectiveDevelopmentDiagnosisNoPolicyFittingOrPromotion',
            authentication='AllConsumedFilesAndSavedRacingStages;NotFullBattleOrLiveHistoryReaudit',
            sources={str(e.root): e.consumed for e in (scientific, admission, publication)}, scientificCloseoutSha256=CLOSEOUT_PIN,
            reviewChargedSeconds=SECONDS, reviewChargedBytes=BYTES,
            totalRecordedChargedSeconds=receipt['totalRecordedChargedSeconds']+SECONDS,
            totalRecordedChargedBytes=receipt['totalRecordedChargedBytes']+BYTES,
            cumulativeDeclaredMaximumSeconds=receipt['cumulativeDeclaredMaximumSeconds']+SECONDS,
            cumulativeDeclaredMaximumBytes=receipt['cumulativeDeclaredMaximumBytes']+BYTES,
            lastVerifiedHistory=dict(values=receipt['liveValues'], files=receipt['historyFiles'], rescannedByThisReview=False),
            secondsBeforeSealing=time.monotonic()-started)
        save('review.json', review)
        (output/Path(__file__).name).write_bytes(source_raw)
        (output/HELPER.name).write_bytes(helper_raw)
        save('files.json', {p.name: sha(p.read_bytes()) for p in sorted(output.iterdir())})
        require(time.monotonic()-started < SECONDS and sum(p.stat().st_size for p in output.iterdir()) < BYTES,
                'Review allowance exhausted')
        print(json.dumps(dict(status='Complete', seconds=time.monotonic()-started,
            retainedBytes=sum(p.stat().st_size for p in output.iterdir()), manifestSha256=sha((output/'files.json').read_bytes()),
            summary=review['summary'], coverage=review['coverage']), indent=2))
    except BaseException as error:
        # The declaration survives even a hard deadline or storage failure.
        if not (output/'failure.json').exists():
            save('failure.json', dict(reason=str(error), chargedSeconds=SECONDS, chargedBytes=BYTES))
        raise
    finally:
        timer.cancel()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=OUT)
    run(parser.parse_args().output)
