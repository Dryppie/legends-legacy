import { Injectable, Signal, signal } from '@angular/core';
import { ApiService } from '../../api/api.service';
import { CraftingProfession, Profession, Recipe } from '../../../../shared/models/profession';
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
    const profession = updated.find((p) => p.professionType === professionType);
    if (!profession) return;

    profession.experience += experience;
    let leveledUp = false;

    while (profession.experience >= profession.experienceUntilNextLevel) {
      profession.experience -= profession.experienceUntilNextLevel;
      profession.level++;
      leveledUp = true;
    }

    this._professions.set(updated);

    if (leveledUp) {
      this.refresh();
    }
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
