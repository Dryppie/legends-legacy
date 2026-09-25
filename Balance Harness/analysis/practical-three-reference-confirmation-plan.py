"""Freeze a five-candidate/three-control confirmation design using saved evidence.

No native executable, combat, entropy or reservation is invoked. Existing plans
and scientific archives are read-only; --check reproduces the deterministic plan.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
import math
from pathlib import Path
from statistics import NormalDist
import time

ROOT=Path(__file__).resolve().parents[2]
PLAN=ROOT/'Balance Harness/Tower-Practical-Three-Reference-Confirmation-Plan.json'
OUT=ROOT/'TestResults/three-reference-confirmation-plan-20260923'
RUN=ROOT/'TestResults/balance/tower-three-reference-tie-comparison-20260923'
REVIEW=ROOT/'TestResults/three-reference-tie-stage-review-20260923'
ADMISSION=ROOT/'TestResults/three-reference-tie-admission-20260923'
EXECUTION=ROOT/'TestResults/three-reference-tie-comparison-execution-20260923'
COST=ROOT/'TestResults/balance/tower-practical-fresh-screening-comparison-20260922'
PINS={RUN:'68f7468ba270f1d51a075772a6fed3d800ad627a1376a00c4829c10ef9d806d1',
      REVIEW:'a2f6ee4e97d0076e1941c66c136608a032aca87d85e1cacc6cdb01a9a495c579',
      ADMISSION:'90b9a5a9815454ba1169d64f61cc8b7a950597cfa56bc1c9380c53155538f1c5',
      EXECUTION:'f05051cf90896bb9d9497a07b32ca207c996886608daa3579b828b1f53bc9507',
      COST:'031c7dfdc14f71ff911fdec94472bd161ef72d99b90882ec23ec93dd3fe38042'}
SOURCE_PINS={'Balance Harness/analysis/practical-confirmation-precision.py':'3404dbbd3beca5d86329a631727e30c0278c44341eb9d495c7255dcfef46d1fa',
             'LL/tools/BalanceHarness/TowerBalanceEvaluator.cs':'1614b92f07d944a1408a88496ce6d9262a4146965847c922a2fb8f165cc23690',
             'LL/tools/BalanceHarness/TowerFixedFamilyConfirmation.cs':'86ee88a97c0258a3fc7e8c8fa553e13147c06e15fd3a38cd92377b54b069f0b6',
             'LL/tools/BalanceHarness/TowerFixedFamilyConfirmationArchive.cs':'32aec177137007f48140e2b1ab535516d763b7102163e5c4c70f4a6fb8fc0607',
             'LL/tools/BalanceHarness/TowerStudyLimits.cs':'54fb94c0779be8c514721f6a10d978bbd7b3e8079bc10bdeef34edfbeeaaeaca'}
COMMON=ROOT/'TestResults/reference-exploration-comparison-admission-20260922/comparison-preparation.py'
COMMON_PIN='edcda4fbbc9f2c19ee9e23ce0ffb15b44f7e29c9596e63982406edd470c0224b'
N,FAMILY,CONTROLS,CANDIDATES=6500,38,3,5
HISTORY,HISTORY_FILES,HISTORY_LIMIT=633313,240,1000000
MIB=1048576
REFS=['399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b',
      '8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50',
      '96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c']
ROOTS=[8,13,15,21,22]


def require(ok,message):
    if not ok: raise ValueError(message)


def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path):
    with path.open('rb') as f: return hashlib.file_digest(f,'sha256').hexdigest()
def digest(value): return hashlib.sha256(json.dumps(value,sort_keys=True,separators=(',',':'),ensure_ascii=True).encode()).hexdigest()
def save(path,value):
    with path.open('x',encoding='utf-8',newline='\n') as f:
        json.dump(value,f,indent=2,allow_nan=False); f.write('\n')
def relative(path): return path.relative_to(ROOT).as_posix()


def module(name,path,pin):
    require(sha(path)==pin,'Changed helper: '+str(path))
    spec=importlib.util.spec_from_file_location(name,path)
    result=importlib.util.module_from_spec(spec); spec.loader.exec_module(result)
    return result


precision=module('three_reference_precision',ROOT/'Balance Harness/analysis/practical-confirmation-precision.py',
                 SOURCE_PINS['Balance Harness/analysis/practical-confirmation-precision.py'])


def power_bound(n,gain=.08,minimum_rate=.25,history=HISTORY_LIMIT):
    require(type(n) is int and n>0 and 0<=history<=HISTORY_LIMIT,'Invalid power input')
    z=precision.critical_value(FAMILY)
    contrast=max(.05,z*math.sqrt(n+z*z)/n)
    viable=.10+z*math.sqrt(.09/n)
    cf=math.exp(-n*(gain-contrast)**2/2) if gain>contrast else 1.
    vf=math.exp(-2*n*(minimum_rate-viable)**2) if minimum_rate>viable else 1.
    iid=max(0.,1-CONTROLS*cf-vf)
    coupling=n*(n-1)/(2*(2**32-history))
    return dict(samplesPerRecipe=n,trueGainAgainstEveryReference=gain,trueCandidateWinRate=minimum_rate,
        historicalExclusionCeiling=history,sufficientGainThreshold=contrast,sufficientViabilityThreshold=viable,
        eachContrastFailureUpper=cf,viabilityFailureUpper=vf,iidJointPassLower=iid,finitePopulationDeduction=coupling,
        uniformWithoutReplacementJointPassLower=max(0.,iid-coupling))


def arithmetic_checks():
    z=precision.critical_value(FAMILY)
    require(abs(z-NormalDist().inv_cdf(1-.025/FAMILY))<1e-8,'Family-38 critical value')
    cells,error=0,0.
    for n,q,delta in [(20,.4,.1),(40,.6,.2),(60,.2,.1)]:
        cuts=precision.thresholds(n,FAMILY); terms=[]
        pg,pl=(q+delta)/2,(q-delta)/2
        for g in range(n+1):
            for l in range(n-g+1):
                passed=20*(g-l)>=n and precision.wilson(g,n,FAMILY)[0]>precision.wilson(l,n,FAMILY)[1]
                require(passed==(g>=cuts[g+l]),'Small multinomial threshold')
                cells+=1
                if passed: terms.append(math.comb(n,g)*math.comb(n-g,l)*pg**g*pl**l*(1-q)**(n-g-l))
        difference=abs(math.fsum(terms)-precision.contrast_probability(n,FAMILY,q,delta,cuts))
        require(difference<1e-12,'Small multinomial probability'); error=max(error,difference)
    cuts=precision.thresholds(N,FAMILY); minimum=1.; feasible=0
    for d in range(N+1):
        first=(d+325+1)//2
        require(cuts[d]==(first if first<=d else d+1),'Changed 325-net-win boundary')
        if first<=d:
            feasible+=1; minimum=min(minimum,precision.wilson(first,N,FAMILY)[0]-precision.wilson(d-first,N,FAMILY)[1])
    bound=power_bound(N)
    for wins in range(N+1):
        require((precision.wilson(wins,N,FAMILY)[0]>=.10)==(wins>=728)
                ==(wins/N>=bound['sufficientViabilityThreshold']),'Changed viability boundary')
    require(bound['uniformWithoutReplacementJointPassLower']>=.8
            and power_bound(6066)['uniformWithoutReplacementJointPassLower']<.8
            and power_bound(6067)['uniformWithoutReplacementJointPassLower']>=.8,'Conditional power boundary')
    require(power_bound(N,.05)['uniformWithoutReplacementJointPassLower']==0
            and power_bound(N,.08,.10)['uniformWithoutReplacementJointPassLower']==0,'Unsupported alternative')
    return dict(status='Passed',family=FAMILY,smallMultinomialCells=cells,maximumProbabilityError=error,
        discordanceThresholds=N+1,feasibleBoundaryRows=feasible,minimumPairedLowerAt325NetWins=minimum,
        viabilityCounts=N+1,minimumViableWins=728,minimumNForSufficientPowerBound=6067)


def fixed_context(scenario):
    value=copy.deepcopy(scenario); value.pop('id'); value['seeds']=[]
    for actor in value['party']: actor['build']['essenceIds']=[]
    return value


def validate_team(team,template):
    s=team['scenario']; party=s['party']
    require(s['seeds']==[] and len(party)==10 and [p['partySlot'] for p in party]==list(range(1,11)), 'Scheduled or incomplete recipe')
    require(fixed_context(s)==fixed_context(template['references'][0]['scenario']),'Changed captured actors, gear, identity or context')
    builds={str(p['partySlot']):p['build']['essenceIds'] for p in party}
    require(digest(builds)==team['partyId'] and all(len(ids)==5 and ids==sorted(set(ids)) for ids in builds.values()),
            'Changed party or canonical ability order')
    allowed={item['id'] for item in template['allowedEssences']}
    require(len(allowed)==len(template['allowedEssences']) and all(set(ids)<=allowed for ids in builds.values()),'Out-of-pool Essence')
    require(team['seedFreeScenarioHash']==digest(s),'Changed complete scenario binding')


def catalogue(template,inventory,review):
    require([s['party']['id'] for s in template['starts']]==REFS and template['ownedCopies'] is None,
            'Changed exact references or inventory context')
    require(inventory['status']=='UnconfirmedInClosedComparison' and inventory['sourceManifestSha256']==PINS[RUN]
            and [c['root'] for c in inventory['candidates']]==ROOTS,'Changed complete candidate set/order')
    expected=[a for a in review['roots'] if not a['selectedReference']]
    require([a['root'] for a in expected]==ROOTS,'Changed diagnostic candidate family')
    recipes=[]
    for entry,a in zip(inventory['candidates'],expected):
        path=RUN/f"study/search-{entry['root']:02d}.json"
        require(Path(entry['sourceCheckpoint']).resolve()==path.resolve(),'Unbound candidate source')
        output=read(path)['baseline']; s=output['scenario']
        require(entry['partyId']==a['selected']==output['finalist']['party']['id']
                and entry['recipeHash']==a['selectedRecipeHash']==output['recipeHash']
                and entry['absoluteConfirmationWins'] is None,'Candidate source mismatch')
        recipes.append(dict(role='Candidate',partyId=entry['partyId'],sourceRoot=entry['root'],
            sourceCheckpoint=relative(path),sourcePhysicalRecipeHash=entry['recipeHash'],scenario=copy.deepcopy(s),seedFreeScenarioHash=digest(s)))
    for i,start in enumerate(template['starts']):
        ref=next(r for r in template['references'] if r['id']==start['referenceId'])
        s=ref['scenario']
        recipes.append(dict(role='PrimaryReference' if i==0 else 'OtherReference',partyId=start['party']['id'],
            referenceId=ref['id'],sourceTemplate=relative(RUN/'template.json'),scenario=copy.deepcopy(s),seedFreeScenarioHash=digest(s)))
    require(len(recipes)==len({r['partyId'] for r in recipes})==8,'Duplicate or missing team')
    for team in recipes: validate_team(team,template)
    return recipes


def resources():
    c=read(COST/'completion.json'); native=read(COST/'native-receipt.json')
    require(c['status']=='Complete' and c['retries']==0 and native['fights']==55672,'Incomplete sizing source')
    audit_seconds=sum(read(COST/name)['seconds'] for name in ['native-audit-process.json','independent-audit-process.json'])
    ratio=52000/55672
    proxy=dict(sourceFights=55672,sourceOwnerSeconds=c['seconds'],sourceOwnerBytes=c['observedBytes'],
        ownerSeconds=c['seconds']*ratio,ownerBytes=math.ceil(c['observedBytes']*ratio),
        nativeSeconds=c['process']['seconds']*ratio,auditSeconds=audit_seconds*ratio,
        method='Linear scaling of the closed 55,672-fight owner, including both audits and publication; deliberately not a performance guarantee.')
    require(2*proxy['ownerSeconds']<7200 and 2*proxy['nativeSeconds']<6000 and 2*proxy['auditSeconds']<1200
            and 2*proxy['ownerBytes']<3072*MIB,'Twofold sizing illustration exceeds proposed envelope')
    return dict(proposedStudyCumulative=dict(seconds=7800,bytes=4096*MIB),
        admissionAllowance=dict(seconds=600,bytes=512*MIB),executionOwner=dict(seconds=7200,bytes=3584*MIB),
        nativeRunMaximum=dict(seconds=6000,bytes=3072*MIB),auditAndPublicationReserve=dict(seconds=1200,bytes=512*MIB),
        fullAdmissionAllowanceCharged=True,fullWorkflowFeasibilityEstablished=False,sizing=proxy,
        ownership='Suspended process assigned to a hidden Windows Job before execution; retain leases and require empty process trees. Audit and publication share the original owner deadline, with no fresh cap.',
        accounting='A new closed study envelope, including full admission allowance and execution/retention/publication. Earlier studies stay separately charged; no refund or transfer. No claim of total project cost.',
        priorEngineeringLedger=dict(seconds=18180,MiB=13584,treatment='Preserved separately as accepted; complete historical engineering totals remain unknown.'),
        implementationRequirement='Measure relevant fixture overhead, capture producing runtime and verify limits before the separate native admission. Include temporary writes, copied runtime/content, journals, logs, cleanup, completion and manifest bytes.')


def build_plan(live=False):
    common=module('three_reference_plan_common',COMMON,COMMON_PIN)
    inventories={path:dict(common.authenticate(path,pin),**{'files.json':pin}) for path,pin in PINS.items()}
    require(all(sha(ROOT/p)==pin for p,pin in SOURCE_PINS.items()),'Changed planning implementation')
    r=read(REVIEW/'review.json'); template=read(RUN/'template.json')
    require(r['status']=='VerifiedThreeReferenceTieStageReview' and r['closedDecision']=='NoSelectorDifferences'
            and r['historyValues']==HISTORY and r['historyFiles']==HISTORY_FILES,'Unverified source review')
    require(read(EXECUTION/'closeout.json')['result']==read(RUN/'result.json')==read(RUN/'independent-audit.json')['result'],
            'Closed scientific result mismatch')
    history=read(REVIEW/'history-files.json')
    require(history==read(EXECUTION/'live-history-files.json') and len(history)==HISTORY_FILES,'Changed saved history inventory')
    if live:
        current,values=common.history(RUN.parent,read(RUN/'request.json'))
        require(current==history and len(values)==HISTORY,'Changed live exclusion history')
    recipes=catalogue(template,read(REVIEW/'selected-challengers.json'),r)
    contrasts=[dict(candidateId=c['partyId'],referenceId=ref['partyId']) for c in recipes[:5] for ref in recipes[5:]]
    require(len(contrasts)==15 and len(recipes)+2*len(contrasts)==FAMILY,'Incorrect family')
    checks=arithmetic_checks(); chunks=[1000]*6+[500]
    plan=dict(version='tower-practical-three-reference-confirmation-plan-v1',status='ProtocolSpecifiedImplementationRequired',
        proposedNativeVersion='tower-practical-three-reference-confirmation-v1',runnableRequest=False,
        question='Which of these five exact selected challengers meet the practical strength criterion against all three retained references?',
        recipesInFixedExecutionOrder=recipes,contrastsInFixedReportingOrder=contrasts,
        designatedPrimaryPartyId=REFS[0],contentHashes=template['contentHashes'],settingsHash=template['settingsHash'],
        sourceExecutionHash=template['executionHash'],budget=template['budget'],ownedCopies=template['ownedCopies'],
        runtimeBinding='Preserve the admitted captured gameplay assemblies, nested dependencies, content and settings. Bind a tested new harness separately after implementation; do not rebuild gameplay from the dirty checkout.',
        samplesPerRecipe=N,maximumAttemptedFights=52000,discoveryFights=0,selectionFights=0,
        endpoint=dict(win='Victory only; draws and defeats are non-wins. Missing, invalid or faulted reports are terminal incompleteness.',
            intervalFamily=FAMILY,familyComposition='8 recipe rates plus 30 gain/loss rates for 15 candidate/reference contrasts.',
            nominalFamilyCoverage=.95,coverageIsApproximate=True,criticalValue=precision.critical_value(FAMILY),
            minimumCandidateWilsonLower=.10,minimumCandidateWins=728,minimumObservedGainAgainstEveryReference=.05,
            minimumNetGainsAgainstEveryReference=325,pairedLowerAgainstEveryReference='strictly positive',
            pairedInterval='[WilsonLower(G)-WilsonUpper(L), WilsonUpper(G)-WilsonLower(L)], n=6500 and family=38 for every rate.',
            qualification='Legal frozen candidate, complete verified family, viability and all three signed contrast gates must pass.',
            reporting='Publish all eight rates, fifteen contrasts and five qualification flags after one final look; report every qualifier in frozen candidate order.',
            positiveDecision='StrongerFixedCandidatesConfirmed',negativeDecision='StrengthNotDemonstrated',
            successRecommendations='Every qualifier in frozen candidate order followed by all three retained references. No new primary or estimated-best winner.',
            completeNegative='Keep all three reference recommendations. Failure to demonstrate strength does not establish equivalence.',
            incomplete='No new recommendation; preserve all exposed values and files. No partial-family qualification.',
            balanceAssessment='NotAssessed',searchReliability='Unresolved',candidateToCandidateSuperiority='NotAssessed'),
        power=dict(target=.80,alternative='One specified candidate has true gain at least eight percentage points against each reference and win probability at least 25%; assumptions, not estimates for these recipes.',
            scope='Lower bound for that one candidate passing all three comparisons and viability, conditional on full panel and operational completion. Implies at least one qualifies if such a candidate exists; not a claim that all five qualify.',
            boundAtConservativeHistory=power_bound(N),boundAtRecordedHistory=power_bound(N,history=HISTORY),
            minimumNForSufficientBound=6067,sampleSizeSensitivity=[power_bound(n) for n in [1000,5500,6000,6066,6067,6100,6500,7000]],
            effectSensitivity=[power_bound(N,gain=g) for g in [.05,.06,.07,.08,.10]],
            assumptions=['Post-freeze cryptographic batch modeled as uniform independent bits; conditional on a full accepted panel, ordered uniform sampling without replacement.',
                'Hoeffding for each contrast in [-1,1] and viability in [0,1], union bound over three comparisons and viability, then one finite-population collision-coupling deduction.',
                'No independence between references or candidates is assumed. No probability is assigned to a useful candidate existing, legal preparation or operational completion.'],
            interpretation='Positive paired lower bounds support positive gain, not true gain exceeding five points. Zero power lower bounds are uninformative.',
            maximumRecipeWilsonHalfWidth=precision.critical_value(FAMILY)/(2*math.sqrt(N+precision.critical_value(FAMILY)**2)),
            maximumPairedIntervalHalfWidth=precision.critical_value(FAMILY)/math.sqrt(N+precision.critical_value(FAMILY)**2)),
        sampling=dict(recordedExclusions=HISTORY,recordedHistoryFiles=HISTORY_FILES,historyLimitIncludingReservations=HISTORY_LIMIT,
            maximumAdmissionHistory=987000,maximumNewReservations=13000,maximumUnionAtRecordedHistory=646313,
            cryptographicFillCalls=1,batchWords=13000,batchBytes=52000,assignedConfirmationValues=N,
            rule='One batch of signed little-endian Int32 words; reject complete history and batch duplicates. Select first 6500 eligible distinct words; permanently reserve every eligible tail value.',
            durability='Under owned registry/output leases, refresh complete history and freeze recipes/protocol/runtime/request before native legality admission and durable entropy intent/start. Persist full batch/completion before cancellation.',
            failure='Shortfall or failure is terminal; no refill, fallback PRNG, retry, resume, replacement, replay or old reserved-value reuse. Unresolved entropy start blocks allocation until separately reconciled.',
            existingOutput='Refuse; never overwrite or resume.',oldScopes='All previous allocations, unused panels and abandoned reservations remain excluded.'),
        execution=dict(implemented=False,nativeScenarioSeedLimit=1000,panelChunkSizes=chunks,transportBindings=56,
            order='Five candidates in source-root order 8,13,15,21,22, then three references in retained order; each consumes the same seven consecutive slices.',
            scenarioPreservation='Only Scenario.Seeds changes in transport. Preserve scenario/build IDs, actors, canonical ability order, equipment, identity, subgroup, time and preparation. Physical-party hashes and whole-scenario hashes are distinct bindings.',
            attempts='Exactly 52000 started/completed ordinal attempts and bound direct reports; no cached outcomes, search, mutation, pruning or interim outcome-dependent stopping. Slices are transport partitions only.',
            audits='Native reconstruction of all 56 inputs and direct reports without combat, plus separate independent recount of eight ordered rows and fifteen contrasts. Validate seed and ordinal identity before pairing.',
            publication='All recipes and reports complete, both audits agree, owned process trees empty, history and inventories rechecked, cumulative receipts sealed; then publish all results and seed-free qualifying exports.',
            compatibility='Separate version/profile. Preserve the existing six-candidate/two-control family-32 commands, exact hashes, resource boundaries and archive semantics.'),
        resources=resources(),
        requiredImplementationChecks=[
            'Exact eight recipes, five/three role boundaries, all fifteen ordered contrasts, full seed-free scenario hashes and captured legal context; reject substitutions, ordering changes and unknown versions.',
            'Family 38, 728 viability and 325 net-gain boundaries, zero/one/multiple qualifiers, every-reference requirement, negative/equal cases and incomplete-family refusal.',
            'Seven common chunks, 56 transport bindings, per-input 1000 limit, 52000 journalled direct reports; reject duplicates, missing/extra/reordered or unbound seeds/reports.',
            'One batch, historical collisions, duplicates, exhausted fresh prefix, full-tail reservation, cancellation and every durable entropy boundary; legacy recovery rejects the new version.',
            'Both read-only audits reproduce results without combat/entropy; failed audits cannot publish; native and independent implementations must not silently share unverified result claims.',
            'Owned process trees, leases, shared deadlines, native/audit/storage reserves, interrupted publication and no retry/resume; preserve legacy fixtures through build/run-tests.ps1.',
            'Capture tested producing runtime and fixture resource receipts, bind plan and final request, then one separately bounded captured-runtime admission before any scientific allocation.'],
        arithmeticChecks=checks,sourceManifests={relative(path):pin for path,pin in PINS.items()},
        planningSourcePins=SOURCE_PINS,commonHelperSha256=COMMON_PIN,sourceCatalogueSha256=sha(REVIEW/'selected-challengers.json'),
        savedHistoryInventorySha256=sha(REVIEW/'history-files.json'),plannerSha256=sha(Path(__file__)),
        newFights=0,newReservations=0,randomDraws=0,nativePreparations=0,currentRecommendationsChanged=False,defaultsChanged=False)
    for path,inventory in inventories.items(): require(common.inventory(path)==inventory,'Evidence changed during planning')
    require(all(sha(ROOT/p)==pin for p,pin in SOURCE_PINS.items()),'Planning source changed')
    return plan


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    mode=parser.add_mutually_exclusive_group(required=True)
    mode.add_argument('--write',action='store_true'); mode.add_argument('--check',type=Path); mode.add_argument('--self-test',action='store_true')
    args=parser.parse_args()
    if args.self_test: print(json.dumps(arithmetic_checks(),indent=2))
    elif args.check:
        p=build_plan(); require(p==read(args.check),'Plan differs from authenticated reproduction')
        print(json.dumps(dict(status='Reproduced',planSha256=sha(args.check),teams=8,contrasts=15,fights=52000)))
    else:
        require(OUT.is_dir() and not PLAN.exists() and not (OUT/'planning.json').exists(),'Use new planning evidence')
        started=time.monotonic(); p=build_plan(live=True); save(PLAN,p)
        save(OUT/'planning.json',dict(status='ThreeReferenceConfirmationPlanVerified',planSha256=sha(PLAN),seconds=time.monotonic()-started,
            historyValues=HISTORY,historyFiles=HISTORY_FILES,liveHistoryPreserved=True,newFights=0,newValues=0,nativePreparations=0,
            sourceManifests=p['sourceManifests'],arithmeticChecks=p['arithmeticChecks']))
        print(json.dumps(dict(status=p['status'],planSha256=sha(PLAN),teams=8,contrasts=15,fights=52000,samplesPerRecipe=N)))
