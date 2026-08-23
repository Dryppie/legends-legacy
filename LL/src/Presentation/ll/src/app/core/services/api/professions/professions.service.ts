import { Injectable, Signal, signal } from '@angular/core';
import { ApiService } from '../../api/api.service';
import {
  CraftingProfession,
  Profession,
  Recipe,
} from '../../../../shared/models/profession';
import {
  CharacterProfession,
  ProfessionType,
} from '../../../../shared/models/Dtos/characterProfession';

@Injectable({
  providedIn: 'root',
})
export class ProfessionsService {
  private readonly _professions = signal<CharacterProfession[]>([]);

  get characterProfessions(): Signal<CharacterProfession[]> {
    return this._professions.asReadonly();
  }

  constructor(private readonly api: ApiService) {}

  refresh(): void {
    this.api.get('profession').subscribe((professions) => {
      this._professions.set([...professions]);
    });
  }

  addExperience(professionType: ProfessionType, experience: number): void {
    const updated = [...this._professions()];
    const professionIndex = updated.findIndex(
      (profession) => profession.professionType === professionType,
    );
    if (experience <= 0) return;
    if (professionIndex < 0) {
      this.refresh();
      return;
    }

    const profession = updated[professionIndex];
    if (
      profession.experienceUntilNextLevel <= 0 ||
      profession.experience + experience >= profession.experienceUntilNextLevel
    ) {
      this.refresh();
      return;
    }

    updated[professionIndex] = {
      ...profession,
      experience: profession.experience + experience,
    };
    this._professions.set(updated);
  }

  getProfession(
    professionType: ProfessionType,
  ): CharacterProfession | undefined {
    return this._professions().find((p) => p.professionType === professionType);
  }

  emitUpdate(): void {
    this._professions.set([...this._professions()]);
  }

  getProfessionById(_id: string): Profession {
    return this.getCraftingProfession();
  }

  getCraftingProfession(): CraftingProfession {
    return {
      name: 'Crafting',
      recipes: this.getCraftingRecipes(),
      iconPath: 'mining',
      professionType: ProfessionType.Crafting,
    };
  }

  getCraftingRecipes(): Recipe[] {
    return [];
  }
}
