import { DropdownRegistryService } from '../../../../core/services/client-side/components/dropdown/dropdown-registry.service';
import { DropdownComponent, DropdownOption } from './dropdown.component';

describe('DropdownComponent pointer selection', () => {
  let component: DropdownComponent<string>;
  let registry: jasmine.SpyObj<DropdownRegistryService>;
  const option: DropdownOption<string> = {
    label: 'Amulet',
    value: 'amulet',
  };

  beforeEach(() => {
    registry = jasmine.createSpyObj<DropdownRegistryService>(
      'DropdownRegistryService',
      ['register', 'clear'],
    );
    component = new DropdownComponent<string>(registry);
    component.open.set(true);
  });

  it('selects immediately for mouse pointers', () => {
    const event = {
      pointerType: 'mouse',
      preventDefault: jasmine.createSpy('preventDefault'),
      stopPropagation: jasmine.createSpy('stopPropagation'),
    } as unknown as PointerEvent;
    const emitted: string[] = [];
    component.selection.subscribe((selection) => emitted.push(selection.main));

    component.onOptionPointerDown(event, option);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(event.stopPropagation).toHaveBeenCalled();
    expect(emitted).toEqual(['amulet']);
    expect(component.open()).toBeFalse();
  });

  it('leaves touch pointers available for scrolling', () => {
    const event = {
      pointerType: 'touch',
      preventDefault: jasmine.createSpy('preventDefault'),
      stopPropagation: jasmine.createSpy('stopPropagation'),
    } as unknown as PointerEvent;
    const emitted: string[] = [];
    component.selection.subscribe((selection) => emitted.push(selection.main));

    component.onOptionPointerDown(event, option);

    expect(event.preventDefault).not.toHaveBeenCalled();
    expect(event.stopPropagation).not.toHaveBeenCalled();
    expect(emitted).toEqual([]);
    expect(component.open()).toBeTrue();
  });
});
