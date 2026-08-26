import { readFileSync, readdirSync, statSync } from 'node:fs';
import { extname, join, relative, resolve } from 'node:path';

const projectRoot = resolve(import.meta.dirname, '..');
const sourceRoot = join(projectRoot, 'src');
const appRoot = join(sourceRoot, 'app');
const failures: string[] = [];

function read(projectRelativePath: string): string {
  return readFileSync(join(projectRoot, projectRelativePath), 'utf8');
}

function assertAbsent(content: string, pattern: RegExp, message: string): void {
  if (pattern.test(content)) failures.push(message);
}

function sourceFiles(directory: string): string[] {
  return readdirSync(directory).flatMap((entry) => {
    const path = join(directory, entry);
    if (statSync(path).isDirectory()) return sourceFiles(path);
    return ['.css', '.html', '.scss', '.ts'].includes(extname(path))
      ? [path]
      : [];
  });
}

const indexHtml = read('src/index.html');
assertAbsent(
  indexHtml,
  /user-scalable\s*=\s*(?:0|no)/i,
  'src/index.html must not disable browser zoom with user-scalable.',
);
assertAbsent(
  indexHtml,
  /maximum-scale\s*=\s*1(?:\.0)?/i,
  'src/index.html must not cap browser zoom at 100%.',
);

const globalStyles = read('src/styles.css');
assertAbsent(
  globalStyles,
  /@apply\s+focus:outline-none/,
  'Global styles must not suppress keyboard focus outlines.',
);
if (!globalStyles.includes('var(--ll-color-focus-ring)')) {
  failures.push('Global styles must use the shared focus-ring token.');
}

const tailwindConfig = read('tailwind.config.js');
if (!tailwindConfig.includes('--ll-color-danger-rgb')) {
  failures.push('Tailwind danger must reference the shared danger token.');
}
if (!tailwindConfig.includes('--ll-color-text-muted-rgb')) {
  failures.push('Tailwind muted colors must reference shared text tokens.');
}
if (!tailwindConfig.includes('--ll-color-text-disabled-rgb')) {
  failures.push('Tailwind disabled colors must reference shared text tokens.');
}

const forbiddenLegacyColors = /#(?:d72e34|6d6d6d)\b/i;
for (const file of sourceFiles(appRoot)) {
  const content = readFileSync(file, 'utf8');
  if (forbiddenLegacyColors.test(content)) {
    failures.push(
      `${relative(projectRoot, file)} uses a retired low-contrast color literal.`,
    );
  }

  const compactTextPatterns = [
    ...content.matchAll(/text-\[(\d*\.?\d+)(px|rem)\]/g),
    ...content.matchAll(/font-size:\s*(\d*\.?\d+)(px|rem)\b/g),
  ];
  for (const match of compactTextPatterns) {
    const size = Number(match[1]);
    const unit = match[2];
    if ((unit === 'px' && size <= 11) || (unit === 'rem' && size < 0.75)) {
      failures.push(
        `${relative(projectRoot, file)} uses fixed compact text (${match[0]}); use --ll-text-xs or a scalable text utility.`,
      );
    }
  }
}

if (failures.length > 0) {
  console.error('Accessibility baseline validation failed:');
  failures.forEach((failure) => console.error(`- ${failure}`));
  process.exitCode = 1;
} else {
  console.log('Accessibility baseline validation passed.');
}
