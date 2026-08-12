import assert from "node:assert/strict";

const alternativePackageManagerPattern =
  /(^|[\s;&|("'`])(?:[^\s;&|"'`]*[\\/])*(?:pnpm|pnpx|yarn|yarnpkg|bun|bunx)(?:\.(?:cmd|exe|ps1|mjs|cjs|js))?(?=$|[\s;&|)"'`])/i;

function invokesAlternativePackageManager(command) {
  return alternativePackageManagerPattern.test(command);
}

function runSelfTest() {
  const blocked = [
    "pnpm install",
    "pnpm.cmd --dir frontend install",
    "corepack pnpm install",
    '"C:\\tools\\pnpm.cmd" exec node --version',
    '& "C:\\tools\\pnpx.ps1" create-app',
    "node C:\\tools\\pnpm.mjs install",
    "yarn install",
    "corepack yarn build",
    "bun install",
    "bunx ng build",
  ];

  const allowed = [
    "npm ci",
    "npm exec ng build",
    'node "C:\\Program Files\\nodejs\\node_modules\\npm\\bin\\npm-cli.js" ci',
    "git diff -- .codex/hooks/enforce-npm-only.mjs",
    'rg "packageManager" package.json',
    "Get-Content package.json",
  ];

  for (const command of blocked) {
    assert.equal(
      invokesAlternativePackageManager(command),
      true,
      `Expected to block: ${command}`,
    );
  }

  for (const command of allowed) {
    assert.equal(
      invokesAlternativePackageManager(command),
      false,
      `Expected to allow: ${command}`,
    );
  }

  process.stdout.write("npm-only hook self-test passed\n");
}

if (process.argv.includes("--self-test")) {
  runSelfTest();
  process.exit(0);
}

let rawInput = "";
for await (const chunk of process.stdin) {
  rawInput += chunk;
}

let hookInput;
try {
  hookInput = JSON.parse(rawInput);
} catch {
  process.stderr.write(
    "The npm-only policy hook could not parse its input; blocking the command.\n",
  );
  process.exit(2);
}

const toolInput = hookInput?.tool_input;
const command =
  typeof toolInput?.command === "string"
    ? toolInput.command
    : typeof toolInput?.cmd === "string"
      ? toolInput.cmd
      : "";

if (invokesAlternativePackageManager(command)) {
  process.stdout.write(
    JSON.stringify({
      hookSpecificOutput: {
        hookEventName: "PreToolUse",
        permissionDecision: "deny",
        permissionDecisionReason:
          "This repository is npm-only. Use npm ci/npm exec; if npm is broken, repair npm instead of switching package managers.",
      },
    }),
  );
}
