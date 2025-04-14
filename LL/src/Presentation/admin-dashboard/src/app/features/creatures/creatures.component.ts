import { Component, OnInit } from '@angular/core';
import { CreatureService } from '../../core/services/creatures/creature.service';
import { AttributeDto, Creature } from '../../shared/models/creature';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl, FormsModule, FormGroup, FormBuilder } from '@angular/forms';
import { AttributeType } from '../../shared/models/enums/attributeType';
import { SplitCamelCasePipe } from '../../shared/pipes/split-camel-case.pipe';

@Component({
  selector: 'app-creatures',
  standalone: true,
  imports: [CommonModule, FormsModule, SplitCamelCasePipe, ReactiveFormsModule],
  templateUrl: './creatures.component.html',
  styleUrl: './creatures.component.css',
})

export class CreaturesComponent implements OnInit {
  creatures: Creature[] = [];
  selectedCreature: Creature | null = null;
  creatureForm!: FormGroup;
  selectedCreatureControl = new FormControl();

  attributeKeys = Object.keys(AttributeType).filter(key => isNaN(Number(key)));

  constructor(private creatureService: CreatureService, private fb: FormBuilder) {}

  ngOnInit(): void {
    this.creatureService.getCreatures().subscribe((creatures) => {
      this.creatures = creatures;
    });

    this.selectedCreatureControl.valueChanges.subscribe((creature) => {
      this.selectedCreature = creature;
      if(creature) {
        this.updateForm(creature);
      }
    });
    console.log(this.creatures);
  }

  updateForm(creature: Creature){
    let formControls: any = {};

    formControls['level'] = new FormControl(creature.level);
    formControls['experienceReward'] = new FormControl(creature.experienceReward);
    formControls['name'] = new FormControl(creature.name);

    this.attributeKeys.forEach((key) => {
      const foundAttribute = creature.baseAttributes.find(attr => AttributeType[attr.attributeType] === key);
      formControls[key] = new FormControl(foundAttribute ? foundAttribute.value : 0);
    });

    this.creatureForm = this.fb.group(formControls);
  }

  saveCreature() {
    if(this.selectedCreature) {
      let updatedAttributes = this.attributeKeys.map(key => ({
        attributeType: AttributeType[key as keyof typeof AttributeType],
        value: this.creatureForm.value[key],
        entityId: this.selectedCreature!.id,
      }));

      this.selectedCreature.level = this.creatureForm.value.level;
      this.selectedCreature.experienceReward = this.creatureForm.value.experienceReward;
      this.selectedCreature.baseAttributes = updatedAttributes;

      this.creatureService.updateCreature(this.createNewCreature()).subscribe({
        next: res => console.log('Creature updated successfully:', res),
        error: err => console.error('Error updating creature:', err),
      });
    }
  }

  createNewCreature(): Creature {

  const attributes: AttributeDto[] = this.attributeKeys.map(key => ({
    attributeType: AttributeType[key as keyof typeof AttributeType],
    value: this.creatureForm.value[key],
    entityId: this.selectedCreature!.id,
  }));

  const newCreature: Creature = {
    id: this.selectedCreature!.id,
    name: this.creatureForm.value.name, // make sure you have a 'name' control in your form
    level: this.creatureForm.value.level,
    experienceReward: this.creatureForm.value.experienceReward,
    baseAttributes: attributes,
  };

  return newCreature;
}

  // Method to select a creature to display its details
  selectCreature(creature: Creature | null): void {
    if(creature){
      this.updateForm(creature);
    } else {
      this.creatureForm.reset();
    }
  }
}
