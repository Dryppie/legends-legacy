"""Bounded read-only inventory of pinned archives; no rewriting, timing or admission."""
import argparse
from collections import defaultdict
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import time

ROOT=Path(__file__).resolve().parents[2]
VERSION='tower-loadout-placement-storage-inventory-v1'
PRIOR='TestResults/loadout-placement-recovery-protocol-handoff-20260924.json'
PRIOR_PIN='e757da5037272dd9d0ea67c0bcb420b938b8b76084ea4d6d58e01898e856a774'
OUTPUT='TestResults/loadout-placement-storage-inventory-20260924'
FAILED='TestResults/loadout-placement-matched-owned-20260924'
PREFIX='tower-proposal-owned-fixture-matched-baseline/result/'
PLACEMENT='TestResults/tower-proposal-owned-fixture-loadout-placement-20260924/result'
SECONDS,BYTES=180,64*1048576
SOURCES={
    'baseline':dict(root=FAILED+'/'+PREFIX.rstrip('/'),manifest=FAILED+'/files.json',prefix=PREFIX,
        pin='7dd1df4ab8ea9d24c53d50cac4137bdb59351a56110e9642cd791474e3f02605',extras={}),
    'placement':dict(root=PLACEMENT,manifest=PLACEMENT+'/files.json',prefix='',
        pin='19283ecbc0f4dc2586d4a95bb70e346949f2cc16845b916298d32ed75dcdb0a6',extras={
            'files.json':'19283ecbc0f4dc2586d4a95bb70e346949f2cc16845b916298d32ed75dcdb0a6',
            'closeout.json':'768819516339b61c987c1ec125777622dcf5b6178fe917188da1587de565c0e4'})}


def require(ok,message):
    if not ok: raise ValueError(message)


def sha(path):
    with path.open('rb') as f: return hashlib.file_digest(f,'sha256').hexdigest()


def read(path):
    def unique(pairs):
        result={}
        for k,v in pairs:
            require(k not in result,'Duplicate JSON key'); result[k]=v
        return result
    return json.loads(path.read_text(encoding='utf-8-sig'),object_pairs_hook=unique,
        parse_constant=lambda _: require(False,'Nonfinite JSON constant'))


def write(path,value):
    path.parent.mkdir(parents=True,exist_ok=True)
    with path.open('x',encoding='utf-8',newline='\n') as f:
        json.dump(value,f,indent=2,allow_nan=False); f.write('\n')


def safe_name(name):
    require(isinstance(name,str) and name and '\\' not in name and ':' not in name
        and not PurePosixPath(name).is_absolute() and name == PurePosixPath(name).as_posix()
        and all(p not in ('','.','..') for p in name.split('/')), 'Unsafe archive member')
    return name


def members(manifest,spec):
    result={safe_name(p[len(spec['prefix']):]):pin for p,pin in manifest.items() if p.startswith(spec['prefix'])}
    for p,pin in spec['extras'].items():
        require(p not in result,'Repeated external member'); result[safe_name(p)]=pin
    require(result and all(isinstance(pin,str) and re.fullmatch('[0-9a-f]{64}',pin) for pin in result.values()),'Invalid source membership or digest')
    return result


def category(name):
    safe_name(name)
    if name.startswith('executable/'): return 'runtime'
    if name.startswith('source/'): return 'source-bindings'
    if '/content/' in '/'+name: return 'captured-content'
    if '/recipes/' in '/'+name: return 'recipes'
    if '/battles/' in '/'+name: return 'battle-reports'
    if name=='attempts.jsonl' or name.endswith('/charges.jsonl'): return 'attempt-journals'
    if name.endswith('/trials.jsonl') or name.endswith('/inputs.jsonl'): return 'trial-bindings'
    if name=='files.json' or name.endswith('/files.json'): return 'manifests'
    if name.startswith('study/heldout-'): return 'heldout-observations'
    if name.startswith('study/pair-'): return 'paired-search-results'
    if name.startswith('study/placement-catalogue-'): return 'placement-catalogues'
    if name=='study/freeze.json': return 'frozen-output-families'
    if '/racing/' in name:
        leaf=PurePosixPath(name).name
        if leaf=='search.json': return 'search-results'
        if leaf=='plan.json': return 'search-plans'
        if leaf.startswith('batch-'): return 'proposal-batches'
        if leaf.startswith('panel-'): return 'frozen-search-panels'
        if leaf.startswith('validation-'): return 'validation'
    return 'other-metadata'


def summarize(rows):
    names=set(); categories=defaultdict(lambda:dict(files=0,bytes=0)); groups=defaultdict(list)
    for row in rows:
        name=safe_name(row['path']); require(name not in names,'Duplicate inventory member'); names.add(name)
        require(type(row['bytes']) is int and row['bytes']>=0 and re.fullmatch('[0-9a-f]{64}',row['sha256']), 'Invalid inventory row')
        require(row['category']==category(name),'Changed category')
        group=categories[row['category']]; group['files']+=1; group['bytes']+=row['bytes']
        groups[row['sha256']].append(row)
    duplicates=[]
    for pin,group in groups.items():
        require(len({r['bytes'] for r in group})==1,'Same digest with different lengths')
        if len(group)>1:
            duplicates.append(dict(sha256=pin,bytesPerCopy=group[0]['bytes'],copies=len(group),
                redundantBytes=(len(group)-1)*group[0]['bytes'],paths=sorted(r['path'] for r in group)))
    duplicates.sort(key=lambda r:(-r['redundantBytes'],r['sha256']))
    total=sum(r['bytes'] for r in rows); repeated=sum(r['redundantBytes'] for r in duplicates)
    return dict(files=len(rows),retainedBytes=total,categories=dict(sorted(categories.items())),
        exactDuplicateGroups=len(duplicates),exactDuplicateBytes=repeated,uniqueWholeFileBytes=total-repeated,
        duplicationScope='Within this archive only; hypothetical gross bytes before any reference/index overhead',
        duplicateGroups=duplicates,largestFiles=sorted(rows,key=lambda r:(-r['bytes'],r['path']))[:20])


def inventory(root,expected,check):
    require(not root.is_symlink() and not root.is_junction(),'Linked archive root')
    paths=[]
    for p in root.rglob('*'):
        require(not p.is_symlink() and not p.is_junction(),'Linked archive member')
        if p.is_file(): paths.append(p)
    require({p.relative_to(root).as_posix() for p in paths}==set(expected),'Changed source membership')
    rows=[]
    for i,p in enumerate(sorted(paths)):
        name=p.relative_to(root).as_posix(); safe_name(name)
        require(p.resolve().is_relative_to(root.resolve()),'Archive member escaped root')
        with p.open('rb') as f:
            digest=hashlib.sha256(); size=0
            while chunk:=f.read(128*1024): digest.update(chunk); size+=len(chunk)
        require(digest.hexdigest()==expected[name],'Changed source bytes: '+name)
        rows.append(dict(path=name,bytes=size,sha256=expected[name],category=category(name)))
        if i%512==0: check()
    return rows


def comparison(a,b):
    aa,bb=summarize(a),summarize(b)
    ad={r['sha256']:r['bytes'] for r in a}; bd={r['sha256']:r['bytes'] for r in b}
    shared=set(ad)&set(bd)
    return dict(version=VERSION,status='AuthenticatedReadOnlyStorageInventory',baseline=aa,placement=bb,
        categoryByteDeltas={k:bb['categories'].get(k,{}).get('bytes',0)-aa['categories'].get(k,{}).get('bytes',0)
            for k in sorted(set(aa['categories'])|set(bb['categories']))},
        crossArchiveUniqueSharedBytes=sum(ad[p] for p in shared),crossArchiveSharedDigests=len(shared),
        crossArchiveSavingsUsableForAdmission=False,
        comparability='Raw retained archives: baseline failed before publication; placement completed. Request counts and terminal metadata differ. No per-report normalization or cost ratio inferred.',
        recoveryGateStillClosed=True,admitted=False,runtimeQualified=False,qualifiedCurrentForecast=None,
        newTimingSamples=0,nativePreparation=False,actualCombat=0,productionEntropyDraws=0,newScientificReservations=0,
        archiveRewrites=0,compressionTrials=0,proposedSavingsAppliedToForecast=False)


def verify(output,pin):
    require(sha(output/'files.json')==pin,'Changed externally pinned inventory package')
    files=read(output/'files.json')
    require({p.relative_to(output).as_posix() for p in output.rglob('*') if p.is_file()}==set(files)|{'files.json'},'Changed inventory package membership')
    for name,digest in files.items(): require(sha(output/safe_name(name))==digest,'Changed retained inventory evidence')
    require(sha(output/'prior-handoff.json')==PRIOR_PIN,'Changed prior gate')
    rows={}
    for label,spec in SOURCES.items():
        require(sha(output/'inputs'/(label+'-manifest.json'))==spec['pin'],'Changed source manifest pin')
        expected=members(read(output/'inputs'/(label+'-manifest.json')),spec)
        rows[label]=read(output/(label+'-inventory.json'))
        require({r['path']:r['sha256'] for r in rows[label]}==expected,'Inventory does not cover authenticated source members')
    expected=comparison(rows['baseline'],rows['placement'])
    require(read(output/'assessment.json')==expected,'Changed storage inventory arithmetic')
    return expected


def create(output):
    require(output==(ROOT/OUTPUT).resolve() and not output.exists(),'Use the single new inventory path; no overwrite')
    require(sha(ROOT/PRIOR)==PRIOR_PIN,'Changed recovery handoff')
    output.mkdir(); started=time.monotonic()
    write(output/'declaration.json',dict(version=VERSION,maximumSeconds=SECONDS,maximumBytes=BYTES,
        chargedSeconds=SECONDS,chargedBytes=BYTES,fullAllowanceChargedAtStart=True,priorHandoffSha256=PRIOR_PIN,
        helperSha256=sha(Path(__file__)),sources=SOURCES,readOnlySources=True,nativePreparation=False,
        newTimingSamples=0,compressionTrials=0,archiveRewrites=0,qualificationStarted=False))
    def check():
        require(time.monotonic()-started<SECONDS and sum(p.stat().st_size for p in output.rglob('*') if p.is_file())<BYTES,
            'Read-only inventory allowance exceeded')
    try:
        prior=read(ROOT/PRIOR)
        for group in ('preservedHistoricalPins','currentSourceHashes'):
            for name,pin in prior[group].items(): require(sha(ROOT/name)==pin,'Changed inherited evidence or implementation: '+name)
        shutil.copyfile(ROOT/PRIOR,output/'prior-handoff.json'); shutil.copyfile(Path(__file__),output/'helper.py')
        rows={}
        for label,spec in SOURCES.items():
            require(sha(ROOT/spec['manifest'])==spec['pin'],'Changed source manifest')
            target=output/'inputs'/(label+'-manifest.json'); target.parent.mkdir(exist_ok=True)
            shutil.copyfile(ROOT/spec['manifest'],target)
            expected=members(read(target),spec)
            rows[label]=inventory(ROOT/spec['root'],expected,check)
            write(output/(label+'-inventory.json'),rows[label]); check()
        assessment=comparison(rows['baseline'],rows['placement']); write(output/'assessment.json',assessment)
        # Both source manifests and source membership are rechecked after traversal.
        for spec in SOURCES.values():
            require(sha(ROOT/spec['manifest'])==spec['pin'],'Source manifest changed during inventory')
            expected=members(read(ROOT/spec['manifest']),spec)
            require({p.relative_to(ROOT/spec['root']).as_posix() for p in (ROOT/spec['root']).rglob('*') if p.is_file()}==set(expected),'Source membership changed during inventory')
        write(output/'completion.json',dict(status='ReadOnlyStorageInventoryComplete',secondsBeforeSealing=time.monotonic()-started,
            chargedSeconds=SECONDS,chargedBytes=BYTES,inheritedPinsAuthenticated=len(prior['preservedHistoricalPins']),
            sourceFilesAuthenticated=sum(len(v) for v in rows.values()),nativePreparation=False,newTimingSamples=0,
            actualCombat=0,productionEntropyDraws=0,archiveRewrites=0,compressionTrials=0,admitted=False))
        check()
    except BaseException as error:
        write(output/'failure.json',dict(reason=str(error),chargedSeconds=SECONDS,chargedBytes=BYTES,
            newTimingSamples=0,archiveRewrites=0,admitted=False)); raise
    finally:
        write(output/'files.json',{p.relative_to(output).as_posix():sha(p) for p in output.rglob('*') if p.is_file()})
    return verify(output,sha(output/'files.json'))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument('action',choices=('create','verify'))
    parser.add_argument('--output',type=Path,required=True); parser.add_argument('--expected-manifest-sha256')
    args=parser.parse_args(); result=create(args.output.resolve()) if args.action=='create' else verify(args.output.resolve(),args.expected_manifest_sha256)
    print(json.dumps({k:v for k,v in result.items() if k not in ('baseline','placement')}|{
        label:{k:v for k,v in result[label].items() if k not in ('duplicateGroups','largestFiles')} for label in ('baseline','placement')},indent=2))
