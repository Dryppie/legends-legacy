"""Independent manifest/request binding for compressed proposal study evidence."""
import json
from pathlib import Path
import re

INDEX = 'evidence-storage.json'
DIRECTORIES = sorted([f'search/root-{n:02d}/{arm}/racing' for n in range(1, 13)
                      for arm in ('control', 'candidate')] + ['study'])
FIELDS = {'version', 'maximumLogicalBytes', 'maximumPhysicalBytes', 'maximumMembers', 'codecSha256', 'readerSha256'}
MAX_BYTES = 5905580032


class Reader:
    def __init__(self, root, selection, freeze, codec, authenticate, work=None):
        self.root, self.selection, self.codec = Path(root).resolve(), selection, codec
        self.work = work
        self.maps, self.pins = {}, {}
        self.codec_physical_bytes_read = self.decoded_bytes_processed = self.decode_passes = 0
        codec.require(set(selection) == FIELDS and selection['version'] == codec.VERSION, 'Invalid storage selection')
        for key, maximum in (('maximumLogicalBytes', MAX_BYTES), ('maximumPhysicalBytes', MAX_BYTES), ('maximumMembers', 72)):
            codec.require(type(selection[key]) is int and 0 < selection[key] <= maximum, 'Invalid trusted storage bound')
        for key in ('codecSha256', 'readerSha256'):
            codec.require(isinstance(selection[key], str) and re.fullmatch('[0-9a-f]{64}', selection[key]), 'Invalid reader module pin')
        self.limits = dict(logicalBytes=selection['maximumLogicalBytes'], physicalBytes=selection['maximumPhysicalBytes'])
        descriptor = self.metadata(self.root / INDEX)
        codec.require(descriptor == dict(selection=selection, indexes=[d+'/'+INDEX for d in DIRECTORIES]), 'Changed request/root storage binding')
        study_names = [f'pair-{n:02d}.json' for n in range(1, 13)] + [
            f"heldout-{f['root']:02d}-{m['recipeHash']}.json" for f in freeze['families'] for m in f['members']]
        logical, physical, count = 0, (self.root / INDEX).stat().st_size, 0
        for relative in DIRECTORIES:
            directory = self.root / relative
            authenticate(directory if relative == 'study' else directory.parent)
            path = directory / INDEX
            index = self.metadata(path)
            codec.require(set(index) == {'version', 'entries'} and index['version'] == selection['version'], 'Mixed evidence index version')
            codec.validate_entries(index['entries'], study_names if relative == 'study' else ['search.json'],
                                   selection['version'], self.limits, selection['maximumMembers'])
            self.maps[relative] = {e['logicalPath']:e for e in index['entries']}
            physical += path.stat().st_size
            for entry in index['entries']:
                logical += entry['logicalBytes']; physical += entry['physicalBytes']; count += 1
                codec.require(not (directory / entry['logicalPath']).exists(), 'Mixed plain/compressed evidence')
                payload = codec._member(directory, entry['physicalPath'])
                codec.require(payload.stat().st_size == entry['physicalBytes'] and self.sha(payload) == entry['physicalSha256'],
                              'Index/manifest payload disagreement')
        codec.require(logical <= selection['maximumLogicalBytes'] and physical <= selection['maximumPhysicalBytes']
                      and count <= selection['maximumMembers'], 'Aggregate evidence budget exceeded')

    def sha(self, path):
        if self.work is not None: return self.work.sha(path)
        import hashlib
        with path.open('rb') as stream:
            return hashlib.file_digest(stream, 'sha256').hexdigest()

    def metadata(self, path):
        self.codec._member(path.parent, path.name)
        self.codec.require(path.stat().st_size <= 65536, 'Oversized evidence storage metadata')
        if self.work is None:
            with path.open(encoding='utf-8') as stream: value = self.codec.strict_json(stream)
        else:
            with self.work.open_read(path) as stream: value = self.work.parse(stream, self.codec.strict_json)
        self.pins[path] = self.sha(path)
        return value

    def relative(self, directory):
        relative = Path(directory).resolve().relative_to(self.root).as_posix()
        self.codec.require(relative in self.maps, 'Unbound evidence directory')
        return relative

    def read(self, directory, name):
        entries = self.maps[self.relative(directory)]
        self.codec.require(name in entries, 'Unindexed evidence member')
        value, work = self.codec.read_json(directory, entries[name], self.selection['version'], self.limits, work=self.work)
        self.codec_physical_bytes_read += work['physicalBytesRead']
        self.decoded_bytes_processed += work['decodedBytesProcessed']
        self.decode_passes += work['decodePasses']
        return value

    def physical_members(self, directory, names):
        entries = self.maps[self.relative(directory)]
        return {entries[n]['physicalPath'] if n in entries else n for n in names} | {INDEX}

    def finish(self):
        for path, pin in self.pins.items():
            self.codec.require(self.sha(path) == pin, 'Storage metadata changed during audit')
