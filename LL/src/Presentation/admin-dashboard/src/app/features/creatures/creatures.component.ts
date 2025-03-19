import { Component, OnInit } from '@angular/core';
import { CreatureService } from '../../core/services/creatures/creature.service';
import { Creature } from '../../shared/models/creature';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AttributeType } from '../../shared/models/enums/attributeType';
import { SplitCamelCasePipe } from '../../shared/pipes/split-camel-case.pipe';

@Component({
  selector: 'app-creatures',
  standalone: true,
  imports: [CommonModule, FormsModule, SplitCamelCasePipe],
  templateUrl: './creatures.component.html',
  styleUrl: './creatures.component.css',
})

export class CreaturesComponent implements OnInit {
  creatures: Creature[] = [];
  selectedCreature: Creature | null = null;

  constructor(private creatureService: CreatureService) {}

  ngOnInit(): void {
    this.creatureService.getCreatures().subscribe((creatures) => {
      // Map each creature's base attributes' attributeType to the corresponding enum string
      this.creatures = creatures.map((creature) => {
        const updatedAttributes = creature.baseAttributes.map((attribute) => {
          // Explicitly cast to ensure TypeScript accepts the conversion
          attribute.attributeType = AttributeType[attribute.attributeType as AttributeType]; 
          return attribute;
        });
        creature.baseAttributes = updatedAttributes;
        return creature;
      });
      console.log(this.creatures); // For debugging, you can check the updated creatures list
    });
  }

  // Method to select a creature to display its details
  selectCreature(creature: Creature | null): void {
    if(creature){
      this.selectedCreature = creature;
    } else {
      this.selectedCreature = null;
    }
  }
}
