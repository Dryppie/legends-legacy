export const administrationActions = [
  { value: 'AccountBanned', label: 'Account banned' },
  { value: 'AccountBanRevoked', label: 'Account ban revoked' },
  { value: 'MultiplayerRestricted', label: 'Multiplayer access restricted' },
  { value: 'MultiplayerRestrictionRevoked', label: 'Multiplayer access restored' },
  { value: 'CompensationItemsGranted', label: 'Compensation items granted' },
  { value: 'CompensationPackageSaved', label: 'Compensation package saved' },
  { value: 'CompensationPackageGranted', label: 'Compensation package granted' },
  { value: 'AlphaSignetsGranted', label: 'Alpha Signets granted' },
  { value: 'AccountRiskStatusChanged', label: 'Investigation status changed' },
  { value: 'AccountRiskNoteAdded', label: 'Investigation note added' },
  { value: 'SupportCaseCreated', label: 'Support case opened' },
  { value: 'SupportCaseUpdated', label: 'Support case updated' },
  { value: 'SupportCaseNoteAdded', label: 'Support note added' },
  { value: 'SupportCaseOperationLinked', label: 'Operation linked to case' },
  { value: 'AuditExported', label: 'Activity log exported' },
  { value: 'StateRefreshDeliveryRetried', label: 'Player state refresh queued again' },
  { value: 'Muted', label: 'Chat muted' },
  { value: 'Unmuted', label: 'Chat mute removed' },
];

export function readableName(value: string): string {
  return value.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/[_:.-]+/g, ' ');
}

export function actionLabel(value: string): string {
  return administrationActions.find(action => action.value === value)?.label ?? readableName(value);
}

export function actionEffect(detailsJson: string): string {
  try {
    const raw = JSON.parse(detailsJson || '{}');
    const details = Object.fromEntries(Object.entries(raw).map(([key, value]) => [key.toLowerCase(), value]));
    if (details['replacementmessageid']) return 'Replacement state-refresh notification queued; player receipt is not confirmed.';
    if (details['packageid']) return `${details['name'] ?? 'Compensation package'} · version ${details['version']}`;
    if (details['quantity'] != null) return `${details['quantity']} × ${details['itemname'] ?? details['itembaseid'] ?? 'Signets'}`;
    if (details['expiresat']) return `Until ${new Date(String(details['expiresat'])).toUTCString()}`;
    if (details['status'] != null) return `Review status: ${readableName(String(details['status']))}`;
    return '';
  } catch { return ''; }
}

export function operationSummary(entry: import('../liveops.models').AdministrationAuditEntry, environment: string): string {
  return ['INTERNAL SUPPORT SUMMARY — review before sharing', actionLabel(entry.actionType), `Environment: ${environment}`, `Outcome: ${entry.outcome}`,
    `Effect: ${actionEffect(entry.detailsJson) || 'See the recorded action in LiveOps.'}`, `Reason: ${entry.reason}`,
    `Operation: ${entry.operationId}`, `Source: ${entry.source}`,
    ...(entry.targetCharacterId ? [`Character: ${entry.targetCharacterId}`] : []),
    ...(entry.targetAccountId ? [`Account: ${entry.targetAccountId}`] : []),
    `Recorded: ${entry.occurredAt}`, 'Acceptance is recorded separately from player-update delivery.'].join('\n');
}
