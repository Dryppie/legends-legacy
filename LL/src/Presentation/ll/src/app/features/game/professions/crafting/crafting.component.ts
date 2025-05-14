import { Component, OnInit } from '@angular/core';
import { ProfessionHeaderComponent } from '../../../../shared/components/professions/profession-header/profession-header.component';
import { NgClass, NgFor, NgIf } from '@angular/common';
import {
  CraftingProfession,
  Recipe,
} from '../../../../shared/models/profession';
import { Subscription } from 'rxjs';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { ActivatedRoute } from '@angular/router';
import { ProfessionsService } from '../../../../core/services/api/professions/professions.service';
import { CharacterActionsService } from '../../../../core/services/api/character-actions/character-actions.service';

@Component({
  selector: 'app-crafting',
  standalone: true,
  imports: [ProfessionHeaderComponent, NgFor, NgIf, NgClass],
  templateUrl: './crafting.component.html',
  styleUrl: './crafting.component.css',
})
export class CraftingComponent implements OnInit {
  filteredRecipes: Recipe[] = [];
  selectedRecipe: Recipe | null = null;
  craftingQueue: Recipe[] = [];

  professionId!: string;
  profession!: CraftingProfession;

  combatStarted = false;
  private subscription: Subscription = new Subscription();
  currentAction: CharacterActionDto | null = null;

  constructor(
    private route: ActivatedRoute,
    private professionService: ProfessionsService,
    private characterActionService: CharacterActionsService,
  ) {}

  ngOnInit(): void {
    this.subscription.add(
      this.characterActionService.currentAction$.subscribe((action) => {
        this.currentAction = action;
      }),
    );
    this.route.paramMap.subscribe((params) => {
      this.professionId = params.get('id') ?? '';
      this.getProfessionDetails(this.professionId);
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  getProfessionDetails(id: string) {
    this.profession = this.professionService.getProfessionById(
      id,
    ) as CraftingProfession;
    this.filteredRecipes = this.profession.recipes;
  }

  selectRecipe(recipe: Recipe) {
    this.selectedRecipe = recipe;
  }

  canCraft(recipe: Recipe): boolean {
    return true;
  }

  craft(recipe: Recipe) {
    throw new Error('Method not implemented.');
  }

  cancelCraft(recipe: Recipe) {
    throw new Error('Method not implemented.');
  }
}
