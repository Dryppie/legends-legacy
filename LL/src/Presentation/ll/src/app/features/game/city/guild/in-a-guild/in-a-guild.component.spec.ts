import { signal } from '@angular/core';
import { CharacterService } from '../../../../../core/services/api/character/character.service';
import { GuildStateService } from '../../../../../core/services/api/guild/guild-state.service';
import { Guild } from '../../../../../shared/models/Dtos/guild/guild';
import { GuildRole } from '../../../../../shared/models/Dtos/guild/guildRole';
import { InAGuildComponent } from './in-a-guild.component';

describe('InAGuildComponent guild description', () => {
  let updateDescription: jasmine.Spy;

  function createComponent(role: GuildRole): InAGuildComponent {
    updateDescription = jasmine.createSpy('updateDescription');
    const state = {
      claimableDailyOrderCount: signal(0),
      shop: signal(null),
      updateDescription,
    } as unknown as GuildStateService;
    const characterService = {
      currentCharacterId: signal('current-character'),
    } as unknown as CharacterService;

    const component = new InAGuildComponent(state, characterService);
    component.guild = createGuild(role);
    return component;
  }

  function createGuild(role: GuildRole): Guild {
    return {
      id: 'guild-id',
      name: 'Test Guild',
      tag: '',
      description: 'Old description',
      guildXp: 0,
      guildLevel: 1,
      maxMembers: 11,
      members: [
        {
          characterId: 'current-character',
          name: 'Current Player',
          level: 10,
          role,
          joinedAt: '2026-08-01T00:00:00Z',
          isOnline: true,
        },
      ],
      invites: [],
      resources: [],
      rolePermissions: [],
      vaultItems: [],
    };
  }

  it('lets leaders save a trimmed description', () => {
    const component = createComponent(GuildRole.Leader);

    component.startEditingDescription();
    expect(component.editingDescription()).toBeTrue();
    expect(component.descriptionDraft).toBe('Old description');

    component.descriptionDraft = '  We raid on weekends.  ';
    component.saveDescription();

    expect(updateDescription).toHaveBeenCalledOnceWith('We raid on weekends.');
    expect(component.editingDescription()).toBeFalse();
  });

  it('lets officers edit the description', () => {
    const component = createComponent(GuildRole.Officer);

    expect(component.canEditDescription).toBeTrue();
  });

  it('does not let plain members edit the description', () => {
    const component = createComponent(GuildRole.Member);

    expect(component.canEditDescription).toBeFalse();

    component.startEditingDescription();
    component.descriptionDraft = 'Nope';
    component.saveDescription();

    expect(component.editingDescription()).toBeFalse();
    expect(updateDescription).not.toHaveBeenCalled();
  });

  it('restores the stored description when editing is cancelled', () => {
    const component = createComponent(GuildRole.Leader);

    component.startEditingDescription();
    component.descriptionDraft = 'Half-written';
    component.cancelEditingDescription();

    expect(component.descriptionDraft).toBe('Old description');
    expect(component.editingDescription()).toBeFalse();
    expect(updateDescription).not.toHaveBeenCalled();
  });

  it('exposes guild Favor from the shop for the header', () => {
    const shop = signal({ guildFavor: 900 });
    const state = {
      claimableDailyOrderCount: signal(0),
      shop,
      updateDescription: jasmine.createSpy('updateDescription'),
    } as unknown as GuildStateService;
    const characterService = {
      currentCharacterId: signal('current-character'),
    } as unknown as CharacterService;
    const component = new InAGuildComponent(state, characterService);

    expect(component.guildFavor()).toBe(900);

    shop.set({ guildFavor: 800 });
    expect(component.guildFavor()).toBe(800);
  });
});
