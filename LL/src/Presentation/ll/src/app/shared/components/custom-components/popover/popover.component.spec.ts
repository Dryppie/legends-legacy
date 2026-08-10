import { PopoverComponent } from './popover.component';

describe('PopoverComponent pointer triggers', () => {
  function createComponent() {
    const component = new PopoverComponent(null!, null!, null!, null!, null!);
    const handleCtrl = jasmine.createSpyObj('handleCtrl', ['requestToggle']);
    (component as any).handleCtrl = handleCtrl;
    return { component, handleCtrl };
  }

  it('toggles a hover popover when its origin is tapped', () => {
    const { component, handleCtrl } = createComponent();
    component.trigger = 'hover';

    component.onOriginPointerDown({ pointerType: 'touch' } as PointerEvent);
    component.onOriginClick({} as MouseEvent);

    expect(handleCtrl.requestToggle).toHaveBeenCalledTimes(1);
  });

  it('leaves mouse clicks to hover behavior', () => {
    const { component, handleCtrl } = createComponent();
    component.trigger = 'hover';

    component.onOriginPointerDown({ pointerType: 'mouse' } as PointerEvent);
    component.onOriginClick({ pointerType: 'mouse' } as PointerEvent);

    expect(handleCtrl.requestToggle).not.toHaveBeenCalled();
  });

  it('does not close a tapped popover from a touch pointerleave', () => {
    const { component } = createComponent();
    component.trigger = 'hover';
    const queueClose = spyOn<any>(component, 'queueClose');

    component.onOriginLeave({ pointerType: 'touch' } as PointerEvent);
    expect(queueClose).not.toHaveBeenCalled();

    component.onOriginLeave({ pointerType: 'mouse' } as PointerEvent);
    expect(queueClose).toHaveBeenCalledTimes(1);
  });
});
