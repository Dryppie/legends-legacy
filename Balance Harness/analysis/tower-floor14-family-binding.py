"""Bind the saved floor-fourteen proposal to an independently qualified catalog.

The ordinary catalog-qualification owner verifies the runtime, accepted catalogs,
input comparisons and historical replays before calling this family validator.
This adapter adds no combat, candidates or seed allocation.
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import re

spec = importlib.util.spec_from_file_location('floor14_bound_family', Path(__file__).with_name('tower-late-floor-limited-defense.py'))
family = importlib.util.module_from_spec(spec)
spec.loader.exec_module(family)
VERSION = 'floor14-limited-defense-bound-family-v1'
SOURCE_MANIFEST = '5a172c2f4a5bdd2ac27aac762a3c0bb82c17514e26ea46dd0ab8542cbb4d43a7'
SOURCE_CELLS = 'ce46b0e8f1fdc1c053b790ffc3ced1418395db84f1d7d4aaebc583f608c9e733'
SAVED_PROPOSAL = '4fc78bdf7ee95e455136d73200a9f649b7e14592041142f774ed10ee1d3081d8'
PARENTS = ['ded5d4151c02dab509bad8fd9d80eda1f950e931ecdbd018841a79915f25089b/retained-armor-and-health', '7eed204832bff54ca37a46429ba20e90381ad669f8cca2c434882c32380f7932/retained-armor-and-health']

FIELDS = {'version', 'status', 'floor', 'source', 'sourceManifestSha256', 'sourceCellsSha256',
          'currentTowerSha256', 'currentAbilitiesSha256', 'savedProposal', 'savedProposalSha256',
          'newSeeds', 'newFights', 'nativePreparations'}


def saved_proposal(binding):
    family.h.check(set(binding) == FIELDS and binding['version'] == VERSION and binding['floor'] == 14 and
                   binding['status'] == 'ProposedNotAdmitted' and
                   binding['newSeeds'] == binding['newFights'] == binding['nativePreparations'] == 0,
                   'Exact unallocated floor-fourteen family binding required')
    family.h.check(binding['sourceManifestSha256'] == SOURCE_MANIFEST and
                   binding['sourceCellsSha256'] == SOURCE_CELLS, 'Original floor-fourteen source required')
    family.h.check(all(isinstance(binding[key], str) and re.fullmatch('[0-9a-f]{64}', binding[key])
                       for key in ('currentTowerSha256', 'currentAbilitiesSha256')),
                   'Qualified catalog SHA-256 pins required')
    family.h.check(Path(binding['source']).is_absolute() and Path(binding['savedProposal']).is_absolute(),
                   'Absolute original source and saved proposal paths required')
    data = Path(binding['savedProposal']).read_bytes()
    family.h.check(hashlib.sha256(data).hexdigest() == binding['savedProposalSha256'] == SAVED_PROPOSAL,
                   'Exact previously saved 183-recipe proposal required')
    return json.loads(data)


def validate(binding, original):
    """Reconstruct all recipes as well as authenticating the saved proposal bytes."""
    return family.validate(saved_proposal(binding), 14, original, PARENTS)


def input_pins(binding):
    """Include both the recipe artifact and every derivation helper in native pins."""
    saved_proposal(binding)
    paths = [Path(binding['savedProposal']), Path(__file__), Path(family.__file__), Path(family.h.__file__)]
    pins = {str(path.resolve()): family.h.sha(path) for path in paths}
    family.h.check(pins[str(Path(binding['savedProposal']).resolve())] == SAVED_PROPOSAL,
                   'Saved proposal changed while binding native inputs')
    return pins
