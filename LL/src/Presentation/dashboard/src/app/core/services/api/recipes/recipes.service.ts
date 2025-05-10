import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { Recipe } from '../../../../shared/models/recipes';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class RecipesService {
  constructor(private apiService: ApiService) {}

  public getRecipes(): Observable<Recipe[]> {
    return this.apiService.get('recipe');
  }

  public updateRecipe(recipe: Recipe): Observable<Recipe> {
    return this.apiService.post('recipe/updateRecipe', recipe);
  }
}
