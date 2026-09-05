import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../api.service';
import { EquipmentProgressionService } from './equipment-progression.service';

describe('EquipmentProgressionService', () => {
  it('loads equipment progression access', () => {
    const api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.returnValue(of([]));
    TestBed.configureTestingModule({ providers: [{ provide: ApiService, useValue: api }] });

    TestBed.inject(EquipmentProgressionService).access().subscribe();

    expect(api.get).toHaveBeenCalledOnceWith('equipmentacquisition/access');
  });
});
