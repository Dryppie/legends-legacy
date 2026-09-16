"""Run existing evidence assertions with bounded parallel hashing, without cross-pass trust caches."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from types import SimpleNamespace
import hashlib
import importlib.util
import re
import threading
import time


class EvidenceVerifier:
    MANIFESTS = {'files.json', 'preparation-files.json', 'final-files.json', 'evidence-files.json'}

    def __init__(self, root, deadline, workers=4):
        if workers not in range(1, 5):
            raise ValueError('Evidence hashing permits one through four workers.')
        self.root = Path(root).resolve()
        self.deadline = deadline
        self.workers = workers
        self.cache = {}
        self.lock = threading.Lock()
        self.pool = ThreadPoolExecutor(max_workers=workers)
        self.reads = self.bytes = self.hits = 0
        self.manifests = []

    def close(self):
        self.pool.shutdown(wait=True, cancel_futures=True)

    def check(self):
        if time.monotonic() >= self.deadline:
            raise TimeoutError('Evidence verification deadline.')

    def path(self, value):
        self.check()
        path = Path(value).resolve()
        if not path.is_relative_to(self.root):
            raise ValueError('Evidence path escapes the frozen root.')
        return path

    @staticmethod
    def stamp(stat):
        return stat.st_dev, stat.st_ino, stat.st_size, stat.st_mtime_ns, stat.st_ctime_ns

    def sha(self, value):
        path = self.path(value)
        before = self.stamp(path.stat())
        with self.lock:
            cached = self.cache.get(path)
            if cached is not None:
                if cached[0] != before:
                    raise ValueError('Evidence changed within a verification pass: ' + str(path))
                self.hits += 1
                return cached[1]
        digest = hashlib.sha256()
        with path.open('rb') as stream:
            while chunk := stream.read(1024 * 1024):
                self.check()
                digest.update(chunk)
        self.check()
        if before != self.stamp(path.stat()):
            raise ValueError('Evidence changed while hashing: ' + str(path))
        result = digest.hexdigest()
        with self.lock:
            # Each manifest submits each path once; overlapping manifests reuse the same pass cache.
            self.cache[path] = (before, result)
            self.reads += 1
            self.bytes += before[2]
        return result

    def read(self, original, value):
        path = self.path(value)
        data = original(value)
        if path.name not in self.MANIFESTS or not isinstance(data, dict):
            return data
        if not all(isinstance(n, str) and isinstance(h, str) and re.fullmatch('[0-9a-fA-F]{64}', h)
                   for n, h in data.items()):
            return data
        started = time.monotonic()
        paths = []
        for name in data:
            member = self.path(path.parent / name)
            if not member.is_relative_to(path.parent):
                raise ValueError('Manifest entry escapes its package.')
            paths.append(member)
        # Bound the queue as well as active reads. The caller retains every original digest assertion.
        for offset in range(0, len(paths), 32):
            self.check()
            futures = [self.pool.submit(self.sha, p) for p in paths[offset:offset + 32]]
            for future in futures:
                future.result(timeout=max(.001, self.deadline - time.monotonic()))
        self.manifests.append(dict(path=str(path), members=len(paths), prehashSeconds=time.monotonic() - started))
        return data

    def load(self, name, path):
        """Wrap only sha/read and recursive local imports; execute the original assertions unchanged."""
        spec = importlib.util.spec_from_file_location(name, self.path(path))
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        self.instrument(module)
        return module

    def instrument(self, module):
        if hasattr(module, 'sha'):
            module.sha = self.sha
        if hasattr(module, 'read'):
            original = module.read
            module.read = lambda value: self.read(original, value)
        if hasattr(module, 'importlib'):
            owner = self

            class Loader:
                def __init__(self, original):
                    self.original = original

                def create_module(self, spec):
                    return self.original.create_module(spec)

                def exec_module(self, loaded):
                    self.original.exec_module(loaded)
                    owner.instrument(loaded)

            def wrapped_spec(name, location, *args, **kwargs):
                spec = importlib.util.spec_from_file_location(name, owner.path(location), *args, **kwargs)
                spec.loader = Loader(spec.loader)
                return spec

            # Avoid mutating Python's shared importlib or any sealed source file.
            module.importlib = SimpleNamespace(util=SimpleNamespace(
                spec_from_file_location=wrapped_spec, module_from_spec=importlib.util.module_from_spec))

    def metrics(self):
        return dict(workers=self.workers, filesHashed=self.reads, bytesHashed=self.bytes,
                    samePassCacheHits=self.hits, manifests=self.manifests, crossPassCache=False)
