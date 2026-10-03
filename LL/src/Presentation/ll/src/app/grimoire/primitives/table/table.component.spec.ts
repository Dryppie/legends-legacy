import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgTableComponent } from './table.component';
import { LgTableHarness } from '../../testing/table.harness';

@Component({
  imports: [LgTableComponent],
  template: `
    <table lgTable density="compact" rhythm="zebra">
      <caption>
        Your listings
      </caption>
      <thead>
        <tr>
          <th scope="col">Item</th>
          <th scope="col" class="lg-table__num">Price each</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <th scope="row">Ashen Blade</th>
          <td class="lg-table__num">1,200</td>
        </tr>
        <tr>
          <th scope="row">Hollow Mail</th>
          <td class="lg-table__num">860</td>
        </tr>
      </tbody>
    </table>
    <table lgTable>
      <tbody>
        <tr>
          <td>Alone</td>
        </tr>
      </tbody>
    </table>
  `,
})
class TableHost {}

describe('LgTableComponent', () => {
  it('is the native table, read by its caption, heads and rows', async () => {
    const fixture = TestBed.createComponent(TableHost);
    fixture.detectChanges();
    const loader = TestbedHarnessEnvironment.loader(fixture);
    const listings = await loader.getHarness(
      LgTableHarness.with({ caption: 'Your listings' }),
    );

    expect(await listings.getHeaders()).toEqual(['Item', 'Price each']);
    expect(await listings.getRows()).toEqual([
      ['Ashen Blade', '1,200'],
      ['Hollow Mail', '860'],
    ]);
    expect(await listings.getDensity()).toBe('compact');
    expect(await listings.isZebra()).toBeTrue();
    expect(fixture.nativeElement.querySelector('table').tagName).toBe('TABLE');
  });

  it('follows its region’s density, with separators between rows, unless told otherwise', async () => {
    const fixture = TestBed.createComponent(TableHost);
    fixture.detectChanges();
    const tables =
      await TestbedHarnessEnvironment.loader(fixture).getAllHarnesses(
        LgTableHarness,
      );

    expect(await tables[1].getDensity()).toBeNull();
    expect(await tables[1].isZebra()).toBeFalse();
    expect(await tables[1].getRows()).toEqual([['Alone']]);
  });
});
