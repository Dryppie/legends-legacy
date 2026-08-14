import { CraftingQueueItem } from '../../models/profession';
import { getEstimatedTemperingQueueDuration } from './tempering-duration.utils';

describe('getEstimatedTemperingQueueDuration', () => {
  it('totals the potential remaining across the complete tempering queue', () => {
    const queue = [queueItem('first', 5), queueItem('second', 8)];

    expect(getEstimatedTemperingQueueDuration(queue)).toBe('2m 10s');
  });

  it('formats an empty queue as zero seconds', () => {
    expect(getEstimatedTemperingQueueDuration([])).toBe('0s');
  });
});

function queueItem(id: string, potential: number): CraftingQueueItem {
  return {
    id,
    equipmentInstance: { id, potential } as CraftingQueueItem['equipmentInstance'],
  };
}
