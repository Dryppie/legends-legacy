import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { AbilityStats } from '../../../models/Dtos/combatResultDto';
import { CombatAbilityNameComponent } from './combat-ability-name.component';

describe('CombatAbilityNameComponent', () => {
  let fixture: ComponentFixture<CombatAbilityNameComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CombatAbilityNameComponent],
      providers: [
        {
          provide: CharacterStateService,
          useValue: { overview: signal(null) },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CombatAbilityNameComponent);
  });

  it('uses a hover popover when the combat payload includes essence details', () => {
    fixture.componentRef.setInput('ability', {
      ...ability(),
      definition: {
        id: 'ability.ember-strike',
        kind: 'Active',
        name: 'Ember Strike',
        description: 'Deals fire damage.',
        cooldownSeconds: 5,
        targets: ['CurrentTarget'],
        tags: ['Damage.Fire'],
        effects: [],
      },
    });

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-popover')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Ember Strike');
  });

  it('keeps uncatalogued combat actions as plain names', () => {
    fixture.componentRef.setInput('ability', ability('Basic Attack'));

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-popover')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Basic Attack');
  });

  function ability(name = 'Ember Strike'): AbilityStats {
    return {
      name,
      totalDamage: 20,
      damageByType: [],
      totalHealing: 0,
      uses: 1,
      hits: 1,
      crits: 0,
      summons: 0,
      stuns: 0,
      selfDamage: 0,
      alliedDamage: 0,
      totalBarrier: 0,
    };
  }
});
