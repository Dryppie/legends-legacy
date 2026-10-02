"""Bind the saved floor-twelve proposal to an independently qualified catalog.

The ordinary catalog-qualification owner verifies the runtime, accepted catalogs,
input comparisons and historical replays before calling this family validator.
This adapter adds no combat, candidates or seed allocation.
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import re

spec = importlib.util.spec_from_file_location('floor12_bound_family', Path(__file__).with_name('tower-floor12-limited-resistance.py'))
family = importlib.util.module_from_spec(spec)
spec.loader.exec_module(family)
VERSION = 'floor12-limited-resistance-bound-family-v1'
SOURCE_MANIFEST = '7facc8455060b9c1c97bb0eb3ab36066bf665a9d288d4146b7ecc2156626e0c5'
SOURCE_CELLS = '4068f5181954657fc12c8cd0f23399251b94ee3530953eae94593062d53adc06'
SAVED_PROPOSAL = '5a88e216284921194d4c63d3889db1c2186d5ed4325128690a80800ad3ff3104'
PARENTS = [
    'generated-finalist/bc90cf078b29d42f5d13970e9516d7cfa85837cd8db1f09b0c6b1e27d115a356',
    'generated-finalist/9ce5bd104e700e465d42e2e5453e1ba990b2ea9bcd900a5dd45b80083560c0a9',
]
FIELDS = {'version', 'status', 'floor', 'source', 'sourceManifestSha256', 'sourceCellsSha256',
          'currentTowerSha256', 'currentAbilitiesSha256', 'savedProposal', 'savedProposalSha256',
          'newSeeds', 'newFights', 'nativePreparations'}


def saved_proposal(binding):
    family.h.check(set(binding) == FIELDS and binding['version'] == VERSION and binding['floor'] == 12 and
                   binding['status'] == 'ProposedNotAdmitted' and
                   binding['newSeeds'] == binding['newFights'] == binding['nativePreparations'] == 0,
                   'Exact unallocated floor-twelve family binding required')
    family.h.check(binding['sourceManifestSha256'] == SOURCE_MANIFEST and
                   binding['sourceCellsSha256'] == SOURCE_CELLS, 'Original floor-twelve source required')
    family.h.check(all(isinstance(binding[key], str) and re.fullmatch('[0-9a-f]{64}', binding[key])
                       for key in ('currentTowerSha256', 'currentAbilitiesSha256')),
                   'Qualified catalog SHA-256 pins required')
    family.h.check(Path(binding['source']).is_absolute() and Path(binding['savedProposal']).is_absolute(),
                   'Absolute original source and saved proposal paths required')
    data = Path(binding['savedProposal']).read_bytes()
    family.h.check(hashlib.sha256(data).hexdigest() == binding['savedProposalSha256'] == SAVED_PROPOSAL,
                   'Exact previously saved 241-recipe proposal required')
    return json.loads(data)


def validate(binding, original):
    """Reconstruct all recipes as well as authenticating the saved proposal bytes."""
    return family.validate(saved_proposal(binding), original, PARENTS)


def input_pins(binding):
    """Include both the recipe artifact and every derivation helper in native pins."""
    saved_proposal(binding)
    paths = [Path(binding['savedProposal']), Path(__file__), Path(family.__file__), Path(family.h.__file__)]
    pins = {str(path.resolve()): family.h.sha(path) for path in paths}
    family.h.check(pins[str(Path(binding['savedProposal']).resolve())] == SAVED_PROPOSAL,
                   'Saved proposal changed while binding native inputs')
    return pins
