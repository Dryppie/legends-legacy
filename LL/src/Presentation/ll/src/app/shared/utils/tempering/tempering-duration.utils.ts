import { CraftingQueueItem } from '../../models/profession';

export const TEMPERING_ACTION_DURATION_SECONDS = 10;

export function getEstimatedTemperingQueueDuration(
  queue: CraftingQueueItem[],
): string {
  const totalSeconds = queue.reduce(
    (sum, item) =>
      sum +
      Math.max(0, item.equipmentInstance.potential ?? 0) *
        TEMPERING_ACTION_DURATION_SECONDS,
    0,
  );

  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  const parts: string[] = [];

  if (hours > 0) parts.push(`${hours}h`);
  if (minutes > 0) parts.push(`${minutes}m`);
  if (seconds > 0 || parts.length === 0) parts.push(`${seconds}s`);

  return parts.join(' ');
}
