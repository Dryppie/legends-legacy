import { Component, Input, OnInit } from '@angular/core';
import { AttributeDto } from '../../models/Dtos/attributesDto';
import { AttributeTypeFormatPipe } from '../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { Contribution, getContributions } from '../../models/attribute-math';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NgFor, NgIf } from '@angular/common';

@Component({
  selector: 'app-attribute-display',
  standalone: true,
  imports: [AttributeTypeFormatPipe, MatTooltipModule, NgIf, NgFor],
  templateUrl: './attribute-display.component.html',
})
export class AttributeDisplayComponent implements OnInit {
  @Input() attribute!: AttributeDto;
  tooltip!: Contribution[];

  ngOnInit(): void {
    this.tooltip = this.setTooltip();
  }

  setTooltip(): Contribution[] {
    return getContributions(this.attribute.attributeType, this.attribute.value);
  }
}
