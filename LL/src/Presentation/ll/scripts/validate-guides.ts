import { readFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { ALL_GUIDE_PAGE_IDS } from '../src/app/shared/help/guide-catalog';

const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const guideRoot = resolve(projectRoot, 'src/assets/help/guides');

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function validateGuide(id: string, value: unknown): void {
  if (!isRecord(value)) throw new Error(`${id}: guide must be an object`);
  if (typeof value['title'] !== 'string' || value['title'].trim() === '') {
    throw new Error(`${id}: title is required`);
  }
  if (
    typeof value['lastReviewed'] !== 'string' ||
    !/^\d{4}-\d{2}-\d{2}$/.test(value['lastReviewed'])
  ) {
    throw new Error(`${id}: lastReviewed must use YYYY-MM-DD`);
  }
  if (!Array.isArray(value['sections']) || value['sections'].length === 0) {
    throw new Error(`${id}: at least one section is required`);
  }

  value['sections'].forEach((section, index) => {
    if (!isRecord(section)) {
      throw new Error(`${id}: section ${index + 1} must be an object`);
    }
    if (
      typeof section['heading'] !== 'string' ||
      section['heading'].trim() === '' ||
      typeof section['body'] !== 'string' ||
      section['body'].trim() === ''
    ) {
      throw new Error(`${id}: section ${index + 1} requires heading and body`);
    }
  });
}

async function run(): Promise<void> {
  await Promise.all(
    ALL_GUIDE_PAGE_IDS.map(async (id) => {
      const path = resolve(guideRoot, `${id}.json`);
      const contents = await readFile(path, 'utf8');
      validateGuide(id, JSON.parse(contents) as unknown);
    }),
  );

  console.log(`Validated ${ALL_GUIDE_PAGE_IDS.length} page guides.`);
}

run().catch((error: unknown) => {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
});
