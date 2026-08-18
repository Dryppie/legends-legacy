import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { SoulstoneUpgradeView } from '../../../../shared/models/soulstones/soulstone-upgrade-view';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { CharacterStateService } from '../character/character-state.service';
import { SoulstoneUpgradeStateService } from './soulstone-upgrade.state.service';
import { SoulstoneUpgradeService } from './soulstone-upgrade.service';

describe('SoulstoneUpgradeStateService', () => {
  it('reacts to the live Soulstone balance without unlocking prerequisites', () => {
    const character = signal(createCharacter(70));
    const api = jasmine.createSpyObj<SoulstoneUpgradeService>(
      'SoulstoneUpgradeService',
      ['getSoulstoneUpgrades', 'upgrade', 'resetSoulstoneUpgrades'],
    );
    api.getSoulstoneUpgrades.and.returnValue(
      of([
        createUpgrade('affordability', 'Not enough Soulstones.'),
        createUpgrade('prerequisite', 'Requires essence_resonance.'),
      ]),
    );
    api.upgrade.and.returnValue(
      of({
        upgrades: [createUpgrade('affordability', 'Not enough Soulstones.')],
        soulstones: 45,
        refundedSoulstones: 0,
      }),
    );
    const updateCharacter = jasmine
      .createSpy('updateCharacter')
      .and.callFake((updated: CharacterDto) => character.set(updated));

    TestBed.configureTestingModule({
      providers: [
        SoulstoneUpgradeStateService,
        { provide: SoulstoneUpgradeService, useValue: api },
        {
          provide: CharacterStateService,
          useValue: {
            currentCharacterId: signal('character-id'),
            currentCharacter: character,
            updateCharacter,
          },
        },
        {
          provide: StateSyncCoordinator,
          useValue: { register: jasmine.createSpy('register') },
        },
      ],
    });

    const state = TestBed.inject(SoulstoneUpgradeStateService);
    state.load();

    expect(state.upgrades()[0].canPurchase).toBeFalse();
    character.set(createCharacter(120));

    expect(state.upgrades()[0].canPurchase).toBeTrue();
    expect(state.upgrades()[0].disabledReason).toBeNull();
    expect(state.upgrades()[1].canPurchase).toBeFalse();
    expect(state.upgrades()[1].disabledReason).toBe(
      'Requires essence_resonance.',
    );

    state.upgrade('affordability');
    expect(api.upgrade).toHaveBeenCalledOnceWith('affordability');
  });
});

function createCharacter(soulstones: number): CharacterDto {
  return {
    id: 'character-id',
    name: 'Character',
    level: 1,
    experience: 0,
    experienceUntilNextLevel: 100,
    cinders: 0,
    soulstones,
    fateEcho: 0,
    sigilFragments: 0,
    guildFavor: 0,
    arenaRating: 0,
  };
}

function createUpgrade(
  id: string,
  disabledReason: string,
): SoulstoneUpgradeView {
  return {
    id,
    branch: 'EssenceArchive',
    displayName: id,
    description: 'Description',
    currentRank: 1,
    maxRank: 5,
    currentEffectText: 'Current',
    nextEffectText: 'Next',
    nextCost: 75,
    canPurchase: false,
    disabledReason,
    appliesTo: [],
    doesNotApplyTo: [],
    refundValue: 25,
    sortOrder: 1,
  };
}
