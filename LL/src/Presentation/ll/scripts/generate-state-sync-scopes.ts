import { readFileSync, writeFileSync } from 'fs';
import path from 'path';

const backendContract = path.resolve(
  __dirname,
  '../../../Core/Application/WebSockets/Contracts/StateSyncScopes.cs',
);
const generatedContract = path.resolve(
  __dirname,
  '../src/app/core/services/real-time/game-realtime/state-sync-scopes.generated.ts',
);

const source = readFileSync(backendContract, 'utf8');
const scopes = [...source.matchAll(/public const string \w+ = "([^"]+)";/g)].map(
  (match) => match[1],
);

if (scopes.length === 0) {
  throw new Error(`No state sync scopes found in ${backendContract}`);
}

const entries = scopes.map((scope) => `  '${scope}',`).join('\n');
const generated = `/* AUTO-GENERATED - DO NOT EDIT.
 * Source: Core/Application/WebSockets/Contracts/StateSyncScopes.cs
 */
export const stateSyncScopes = [
${entries}
] as const;

export type StateSyncScope = (typeof stateSyncScopes)[number];
export type StateVersionMap = Readonly<
  Partial<Record<StateSyncScope, number>>
>;

const stateSyncScopeSet: ReadonlySet<string> = new Set(stateSyncScopes);

export function isStateSyncScope(value: string): value is StateSyncScope {
  return stateSyncScopeSet.has(value);
}
`;

writeFileSync(generatedContract, generated);
