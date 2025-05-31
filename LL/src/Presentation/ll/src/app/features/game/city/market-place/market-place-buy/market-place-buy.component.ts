import { NgFor, NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-market-place-buy',
  standalone: true,
  imports: [NgFor, NgIf, FormsModule, ReactiveFormsModule],
  templateUrl: './market-place-buy.component.html',
  styleUrl: './market-place-buy.component.css',
})
export class MarketPlaceBuyComponent {
  // allItems: Item[] = [];
  // /** two-way-bound filters */
  // filters = {
  //   search: '',
  //   category: '',
  //   rarity: '',
  //   priceSort: 'asc' as 'asc' | 'desc',
  // };
  // /** derived view */
  // get filteredItems(): Item[] {
  //   let out = [...this.allItems];
  //   // search
  //   if (this.filters.search) {
  //     const q = this.filters.search.toLowerCase();
  //     out = out.filter((i) => i.name.toLowerCase().includes(q));
  //   }
  //   // category & rarity
  //   ['category', 'rarity'].forEach((key) => {
  //     const v = this.filters[key as 'category' | 'rarity'];
  //     if (v) out = out.filter((i) => i[key] === v);
  //   });
  //   // sort
  //   out.sort((a, b) =>
  //     this.filters.priceSort === 'asc' ? a.price - b.price : b.price - a.price,
  //   );
  //   return out;
  // }
  // /** dropdown data (could also come from enum/service) */
  // categories = ['Weapon', 'Armor', 'Potion', 'Misc'];
  // rarities = ['Common', 'Uncommon', 'Rare', 'Epic', 'Legendary'];
  // /** selected item for modal */
  // selected: Item | null = null;
  // openConfirm(i: Item) {
  //   this.selected = i;
  // }
  // /** helpers */
  // resetFilters() {
  //   this.filters = { search: '', category: '', rarity: '', priceSort: 'asc' };
  // }
  // applyFilters() {
  //   /* noop: getter handles it */
  // }
}
