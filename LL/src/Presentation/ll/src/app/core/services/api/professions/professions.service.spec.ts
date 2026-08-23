import { of } from 'rxjs';
import {
  CharacterProfession,
  ProfessionType,
} from '../../../../shared/models/Dtos/characterProfession';
import { ApiService } from '../../api/api.service';
import { ProfessionsService } from './professions.service';

describe('ProfessionsService', () => {
  it('refreshes canonical state when combat XP arrives before professions are loaded', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.returnValue(of([profession(0)]));
    const service = new ProfessionsService(api);

    service.addExperience(ProfessionType.Mining, 50);

    expect(api.get).toHaveBeenCalledOnceWith('profession');
    expect(service.getProfession(ProfessionType.Mining)?.experience).toBe(0);
  });

  it('updates below a level boundary and refreshes at the boundary', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.returnValues(of([profession(100)]), of([profession(0, 2)]));
    const service = new ProfessionsService(api);
    service.refresh();

    service.addExperience(ProfessionType.Mining, 50);
    expect(service.getProfession(ProfessionType.Mining)?.experience).toBe(150);

    service.addExperience(ProfessionType.Mining, 324);
    expect(api.get).toHaveBeenCalledTimes(2);
    expect(service.getProfession(ProfessionType.Mining)?.level).toBe(2);
  });
});

function profession(experience: number, level = 1): CharacterProfession {
  return {
    professionType: ProfessionType.Mining,
    level,
    experience,
    experienceUntilNextLevel: level === 1 ? 474 : 1_896,
  };
}
