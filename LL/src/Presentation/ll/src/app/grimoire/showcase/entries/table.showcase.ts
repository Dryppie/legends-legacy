import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LG_PANEL, LgTableComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

const LISTINGS = [
  {
    item: 'Ashen Blade',
    seller: 'Maren',
    qty: 1,
    price: '1,200',
    ends: '2h 14m',
  },
  {
    item: 'Hollow Mail',
    seller: 'Oswin',
    qty: 1,
    price: '860',
    ends: '5h 02m',
  },
  {
    item: 'Essence Dust',
    seller: 'Tamsin',
    qty: 40,
    price: '12',
    ends: '1d 3h',
  },
  {
    item: 'Cinder Ward',
    seller: 'Kaelen',
    qty: 2,
    price: '3,450',
    ends: '18m',
  },
];

@Component({
  selector: 'sc-table-showcase',
  imports: [ShowcaseStoryDirective, LgTableComponent, ...LG_PANEL],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Default"
      width="36rem"
      notes="The native table in the region's density: label-style heads over a hairline, body-compact rows parted by hairlines, numbers tabular at the end, the unit named once in the head."
    >
      <table lgTable>
        <caption>
          Your listings
        </caption>
        <thead>
          <tr>
            <th scope="col">Item</th>
            <th scope="col">Seller</th>
            <th scope="col" class="lg-table__num">Qty</th>
            <th scope="col" class="lg-table__num">Price each</th>
          </tr>
        </thead>
        <tbody>
          @for (l of listings; track l.item) {
            <tr>
              <th scope="row">{{ l.item }}</th>
              <td>{{ l.seller }}</td>
              <td class="lg-table__num">{{ l.qty }}</td>
              <td class="lg-table__num">{{ l.price }}</td>
            </tr>
          }
        </tbody>
      </table>
    </ng-template>

    <ng-template
      scStory="Compact and zebra"
      width="36rem"
      notes="density=compact for an order book; rhythm=zebra stripes the rows on row-stripe instead of hairlines, never both."
    >
      <table lgTable density="compact" rhythm="zebra">
        <thead>
          <tr>
            <th scope="col">Item</th>
            <th scope="col">Seller</th>
            <th scope="col" class="lg-table__num">Price each</th>
            <th scope="col" class="lg-table__num">Ends in</th>
          </tr>
        </thead>
        <tbody>
          @for (l of listings; track l.item) {
            <tr>
              <th scope="row">{{ l.item }}</th>
              <td>{{ l.seller }}</td>
              <td class="lg-table__num">{{ l.price }}</td>
              <td class="lg-table__num">{{ l.ends }}</td>
            </tr>
          }
        </tbody>
      </table>
    </ng-template>

    <ng-template
      scStory="In a Panel"
      width="30rem"
      notes="A flush Panel: the table's cells pad to the Panel's inset, so the first column lines up with its title."
    >
      <lg-panel flush>
        <lg-panel-header
          ><lg-panel-title>Guild members</lg-panel-title></lg-panel-header
        >
        <table lgTable>
          <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col" class="lg-table__num">Level</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <th scope="row">Maren</th>
              <td class="lg-table__num">42</td>
            </tr>
            <tr>
              <th scope="row">Oswin</th>
              <td class="lg-table__num">38</td>
            </tr>
            <tr>
              <th scope="row">Tamsin</th>
              <td class="lg-table__num">17</td>
            </tr>
          </tbody>
        </table>
      </lg-panel>
    </ng-template>
  `,
})
export class TableShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly listings = LISTINGS;
}

export const TABLE_SHOWCASE: ShowcaseEntry = {
  slug: 'table',
  name: 'Table',
  tier: 'primitives',
  summary:
    'The data table: native cells in the region’s type, density and row rhythm.',
  covers: ['LgTableComponent'],
  readme: 'src/app/grimoire/primitives/table/README.md',
  component: TableShowcaseComponent,
};
