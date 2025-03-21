import { Component, OnInit } from '@angular/core';
import { CreatureService } from '../../core/services/creatures/creature.service';
import { Creature } from '../../shared/models/creature';
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
        value: this.creatureForm.value[key]
      }));

      this.selectedCreature.level = this.creatureForm.value.level;
      this.selectedCreature.experienceReward = this.creatureForm.value.experienceReward;
      this.selectedCreature.baseAttributes = updatedAttributes;
      console.log("Updated Creature:", this.selectedCreature);
    }
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
