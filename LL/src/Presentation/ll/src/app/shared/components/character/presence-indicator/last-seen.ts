/**
 * How long ago a character was last active, for Presence: "just now", "5 minutes ago", "3 hours ago", "2 days ago".
 * Null when the time is missing or unreadable.
 */
export function lastSeenLabel(lastSeenAt: string | null | undefined): string | null {
  if (!lastSeenAt) return null;

  const timestamp = new Date(lastSeenAt).getTime();
  if (Number.isNaN(timestamp)) return null;

  const elapsedMinutes = Math.floor(
    Math.max(0, Date.now() - timestamp) / 60_000,
  );
  if (elapsedMinutes < 1) return 'just now';
  if (elapsedMinutes < 60) {
    return `${elapsedMinutes} ${elapsedMinutes === 1 ? 'minute' : 'minutes'} ago`;
  }

  const elapsedHours = Math.floor(elapsedMinutes / 60);
  if (elapsedHours < 24) {
    return `${elapsedHours} ${elapsedHours === 1 ? 'hour' : 'hours'} ago`;
  }

  const elapsedDays = Math.floor(elapsedHours / 24);
  return `${elapsedDays} ${elapsedDays === 1 ? 'day' : 'days'} ago`;
}
