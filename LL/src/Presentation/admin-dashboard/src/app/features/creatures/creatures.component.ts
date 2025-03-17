import { Component, OnInit } from '@angular/core';
import { CreatureService } from '../../core/services/creatures/creature.service';
import { Creature } from '../../shared/models/creature';

@Component({
  selector: 'app-creatures',
  standalone: true,
  imports: [],
  templateUrl: './creatures.component.html',
  styleUrl: './creatures.component.css',
})
export class CreaturesComponent implements OnInit {
  creatures: Creature[] = [];
  constructor(private creatureService: CreatureService) {}

  ngOnInit(): void {
    this.creatureService.getCreatures().subscribe((creatures) => {
      this.creatures = creatures;
      console.log(this.creatures);
    });
  }
}
