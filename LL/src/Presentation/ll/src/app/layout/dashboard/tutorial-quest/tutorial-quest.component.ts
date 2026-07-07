import { NgIf } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { TutorialStateService } from '../../../core/services/api/tutorial/tutorial-state.service';
import { TutorialPresenterService } from '../../../core/services/api/tutorial/tutorial-presenter.service';

@Component({
  selector: 'app-tutorial-quest',
  standalone: true,
  imports: [NgIf],
  templateUrl: './tutorial-quest.component.html',
})
export class TutorialQuestComponent implements OnInit {
  private readonly tutorialState = inject(TutorialStateService);
  private readonly presenter = inject(TutorialPresenterService);

  readonly state = this.tutorialState.state;
  readonly visible = this.tutorialState.visible;
  readonly loading = this.tutorialState.loading;
  readonly error = this.tutorialState.error;

  ngOnInit(): void {
    this.presenter.initialize();
  }

  go(): void {
    this.tutorialState.navigateToCurrentStep();
  }
}
