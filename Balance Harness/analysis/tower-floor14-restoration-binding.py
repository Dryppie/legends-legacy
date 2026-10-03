"""Bind the saved 198-recipe Restoration family through the original qualification.

The existing 73-recipe catalog qualification and 183-recipe armor binding
remain mandatory. This adapter adds exact recipes, never fights or a candidate.
"""
import importlib.util
import json
from pathlib import Path


def module(name, filename):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(filename))
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


base = module('floor14_restoration_previous_binding', 'tower-floor14-family-binding.py')
family = module('floor14_restoration_saved_family', 'tower-floor14-limited-restoration.py')
VERSION = 'floor14-limited-restoration-bound-family-v1'
SAVED_PROPOSAL = '889ffabb26f8cf8ebfee3aa21307b23fceba7dd560773ad74e3cd8c012dac556'
FIELDS = base.FIELDS | {'armorProposal', 'armorProposalSha256'}


def previous_binding(binding):
    family.h.check(set(binding) == FIELDS and binding['version'] == VERSION,
                   'Exact expanded floor-fourteen binding required')
    previous = {k: binding[k] for k in base.FIELDS}
    previous.update(version=base.VERSION, savedProposal=binding['armorProposal'],
                    savedProposalSha256=binding['armorProposalSha256'])
    return previous


def saved_proposal(binding):
    # Keep all original source, progression, status and artifact requirements.
    base.saved_proposal(previous_binding(binding))
    path = Path(binding['savedProposal'])
    family.h.check(path.is_absolute() and family.h.sha(path) == binding['savedProposalSha256'] == SAVED_PROPOSAL,
                   'Exact saved 198-recipe Restoration proposal required')
    return json.loads(path.read_text(encoding='utf-8'))


def validate(binding, original):
    proposal = saved_proposal(binding)
    armor = base.validate(previous_binding(binding), original)
    return family.validate(proposal, armor)


def input_pins(binding):
    saved_proposal(binding)
    pins = base.input_pins(previous_binding(binding))
    paths = [Path(binding['savedProposal']), Path(__file__), Path(family.__file__), Path(family.h.__file__)]
    pins.update({str(path.resolve()): family.h.sha(path) for path in paths})
    family.h.check(pins[str(Path(binding['savedProposal']).resolve())] == SAVED_PROPOSAL,
                   'Saved Restoration proposal changed during binding')
    return pins
