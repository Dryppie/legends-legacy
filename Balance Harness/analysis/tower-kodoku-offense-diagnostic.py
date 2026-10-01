"""Fixed 186-recipe 0.60 offense diagnostic; never acceptance evidence."""
import importlib.util
from pathlib import Path

s=importlib.util.spec_from_file_location('offense_diagnostic_base',Path(__file__).with_name('tower-eight-item-diagnostic.py'))
base=importlib.util.module_from_spec(s);s.loader.exec_module(base);h=base.h
DIAGNOSTIC='floor8-kodoku-offense-diagnostic-v1'
PROVENANCE='kodoku-offense-diagnostic-provenance.json'
LIMITS=base.LIMITS


def admit_proposal(io,path):
    p=h.read(path);source=Path(p['source'])
    h.check(p['version']=='floor8-kodoku-offense-proposal-v1' and p['status']=='ProposedNotPrepared' and p['floor']==8 and
            p['newFights']==p['newSeeds']==p['nativePreparations']==0 and p['candidateMaterialized'] is False,'Unallocated offense proposal required')
    h.check(p['proposedDiagnostic']==LIMITS and p['equipmentEligibility']==h.EQUIPMENT and
            (p['familySize'],p['actualCompositions'],p['eligibleRecipes'])==(186,9,128),'Proposal limits changed')
    h.check(h.sha(source/'files.json')==p['sourceManifestSha256'] and h.sha(source/'cells.json')==p['sourceCellsSha256'],'Proposal source changed')
    io.authenticate(source);q=h.read(source/'request.json')
    h.check(q['mode']=='prepare' and q['maximumFights']==0 and not q['seeds'] and h.read(source/'completion.json')['status']=='Complete','Original zero-fight source required')
    cells=h.read(source/'cells.json')
    h.check(len(cells)==len({c['id'] for c in cells})==186 and len({h.composition(c) for c in cells})==9 and
            sum(len(h.specialized(c))<=8 and len(set(h.specialized(c)))<=2 for c in cells)==128 and
            all(c['scenario']['floorNumber']==8 and not c['scenario']['seeds'] for c in cells),'Incomplete unchanged family')
    h.check(h.sha(p['precedingEvidence'])==p['precedingEvidenceSha256'] and h.sha(p['savedReview'])==p['savedReviewSha256'],'Diagnostic evidence changed')
    e=h.read(p['precedingEvidence']);review=h.read(p['savedReview'])
    h.check(e['status']=='Verified' and e['diagnosticOnly'] is True and e['usedForAcceptance'] is False and
            [v['eightItems']['wins'] for v in e['comparisons']]==[15,13,16] and
            review['status']=='VerifiedSavedReportReview' and review['evidenceSha256']==p['precedingEvidenceSha256'] and
            review['newFights']==review['newSeeds']==0 and review['usedForAcceptance'] is False,'Closed descriptive source required')
    io.ability_module().kodoku_offense_module().expected(source/'content',p['candidate'],8)
    return p,cells


def validate_panel(mode,floor,samples,version,family=None):
    h.check(version==DIAGNOSTIC and mode=='screen' and floor==8 and samples==8 and family==186,'Only the exact offense diagnostic admits eight seeds')


def validate_contract(d,cells):
    h.check(d['version']==DIAGNOSTIC and d['limits']==LIMITS and d['floor']==8 and d['familySize']==186,'Offense diagnostic limits changed')
    h.check(len(cells)==186 and d['cellsSha256']==h.sha(Path(d['source'])/'cells.json'),'Declared family changed')
    h.check(len(d['studies'])==len(set(d['studies']))==2 and all(Path(p).is_absolute() for p in d['studies']),'Two distinct absolute diagnostic paths required')
    h.check((d['initialExclusions'],d['eligibleRecipes'],d['actualCompositions'])==(924220,128,9),'Offense diagnostic context changed')


def read_contract(io,path,pin):
    h.check(pin is not None and h.sha(path)==pin,'Diagnostic declaration pin required')
    d=h.read(path);source=Path(d['source']);cells=h.read(source/'cells.json');validate_contract(d,cells)
    for group in ('sourcePins','liveCatalogPins','ledgerPins'):
        for member,expected in d[group].items():h.check(h.sha(member)==expected,'Diagnostic input changed: '+member)
    h.check(h.sha(d['proposal'])==d['proposalSha256'],'Proposal changed')
    p,expected=admit_proposal(io,Path(d['proposal']))
    h.check(source.resolve()==Path(p['source']).resolve() and cells==expected and d['candidatePlan']==p['candidate'],'Exact source or candidate changed')
    return d,cells


def audit_request(io,output,q):
    return base.audit_request(io,output,q,contract_reader=read_contract,panel_validator=validate_panel,provenance_name=PROVENANCE)


def admit_batch(*args):
    return base.admit_batch(*args,contract_reader=read_contract,panel_validator=validate_panel,provenance_name=PROVENANCE)
