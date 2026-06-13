import { mkdirSync, writeFileSync } from 'fs';
import path from 'path';

const targetDir = path.resolve(__dirname, '../src/assets');
const versionJsonTarget = path.join(targetDir, 'version.json');
const appVersionTarget = path.resolve(
  __dirname,
  '../src/app/core/app-version.ts',
);

const version = {
  version: process.env.BUILD_VERSION ?? new Date().toISOString(),
};

mkdirSync(targetDir, { recursive: true });
writeFileSync(versionJsonTarget, `${JSON.stringify(version, null, 2)}\n`);
writeFileSync(
  appVersionTarget,
  `/* AUTO-GENERATED - DO NOT EDIT */\nexport const APP_VERSION = '${version.version}';\n`,
);
