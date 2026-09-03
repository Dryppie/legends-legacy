import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../api.service';
import { EquipmentProgressionService } from './equipment-progression.service';

describe('EquipmentProgressionService', () => {
  it('uses the normal equipment route for starter choices', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.returnValue(of([]));
    TestBed.configureTestingModule({ providers: [{ provide: ApiService, useValue: api }] });

    TestBed.inject(EquipmentProgressionService).starters().subscribe();

    expect(api.get).toHaveBeenCalledOnceWith('equipment/starter-options');
  });
});
