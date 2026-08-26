import { PopoverComponent } from './popover.component';

describe('PopoverComponent pointer triggers', () => {
  function createComponent() {
    const component = new PopoverComponent(null!, null!, null!, null!, null!);
    const handleCtrl = jasmine.createSpyObj('handleCtrl', [
      'requestToggle',
      'requestOpen',
      'requestClose',
    ]);
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

  it('provides an explicit close action for a touch-opened popover', () => {
    const { component, handleCtrl } = createComponent();

    component.close();

    expect(handleCtrl.requestClose).toHaveBeenCalledOnceWith();
  });

  it('toggles a click popover from the keyboard', () => {
    const { component, handleCtrl } = createComponent();
    component.trigger = 'click';
    const event = new KeyboardEvent('keydown', { key: 'Enter' });
    spyOn(event, 'preventDefault');

    component.onOriginKeydown(event);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(handleCtrl.requestToggle).toHaveBeenCalledOnceWith();
  });

  it('closes an open popover when Escape is pressed', () => {
    const { component, handleCtrl } = createComponent();

    component.onOriginKeydown(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(handleCtrl.requestClose).toHaveBeenCalledOnceWith();
  });
});
