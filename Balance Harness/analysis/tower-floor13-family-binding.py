"""Bind the saved floor-thirteen proposal to an independently qualified catalog.

The ordinary catalog-qualification owner verifies the runtime, accepted catalogs,
input comparisons and historical replays before calling this family validator.
This adapter adds no combat, candidates or seed allocation.
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import re

spec = importlib.util.spec_from_file_location('floor13_bound_family', Path(__file__).with_name('tower-late-floor-limited-defense.py'))
family = importlib.util.module_from_spec(spec)
spec.loader.exec_module(family)
VERSION = 'floor13-limited-defense-bound-family-v1'
SOURCE_MANIFEST = 'af56729b3f475edad39f99766bc8f64fdd4d1a789900c686fa361ec97f2685ac'
SOURCE_CELLS = '851ba31632a443a6db364edd663c0a395240a1c25557f4eb2fdccce11a39ca9f'
SAVED_PROPOSAL = '97c81d7cb3aa6ad5b8f7f3b8b77ad574ca5e586ce8a1b662ee3995e728836225'
PARENTS = [
    'generated-finalist/f82246fa1e186af4362d0c047219be7e171fa1c43d806b7cc37b31b140a95b64',
    'generated-finalist/41f84fffebe9952813b7326e56db8af1bbec241135eb07af1a3d146efcfae0e3',
]
FIELDS = {'version', 'status', 'floor', 'source', 'sourceManifestSha256', 'sourceCellsSha256',
          'currentTowerSha256', 'currentAbilitiesSha256', 'savedProposal', 'savedProposalSha256',
          'newSeeds', 'newFights', 'nativePreparations'}


def saved_proposal(binding):
    family.h.check(set(binding) == FIELDS and binding['version'] == VERSION and binding['floor'] == 13 and
                   binding['status'] == 'ProposedNotAdmitted' and
                   binding['newSeeds'] == binding['newFights'] == binding['nativePreparations'] == 0,
                   'Exact unallocated floor-thirteen family binding required')
    family.h.check(binding['sourceManifestSha256'] == SOURCE_MANIFEST and
                   binding['sourceCellsSha256'] == SOURCE_CELLS, 'Original floor-thirteen source required')
    family.h.check(all(isinstance(binding[key], str) and re.fullmatch('[0-9a-f]{64}', binding[key])
                       for key in ('currentTowerSha256', 'currentAbilitiesSha256')),
                   'Qualified catalog SHA-256 pins required')
    family.h.check(Path(binding['source']).is_absolute() and Path(binding['savedProposal']).is_absolute(),
                   'Absolute original source and saved proposal paths required')
    data = Path(binding['savedProposal']).read_bytes()
    family.h.check(hashlib.sha256(data).hexdigest() == binding['savedProposalSha256'] == SAVED_PROPOSAL,
                   'Exact previously saved 240-recipe proposal required')
    return json.loads(data)


def validate(binding, original):
    """Reconstruct all recipes as well as authenticating the saved proposal bytes."""
    return family.validate(saved_proposal(binding), 13, original, PARENTS)


def input_pins(binding):
    """Include both the recipe artifact and every derivation helper in native pins."""
    saved_proposal(binding)
    paths = [Path(binding['savedProposal']), Path(__file__), Path(family.__file__), Path(family.h.__file__)]
    pins = {str(path.resolve()): family.h.sha(path) for path in paths}
    family.h.check(pins[str(Path(binding['savedProposal']).resolve())] == SAVED_PROPOSAL,
                   'Saved proposal changed while binding native inputs')
    return pins
