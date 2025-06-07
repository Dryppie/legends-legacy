import {
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
} from '@angular/core';
import { CharacterBadgeComponent } from '../../../shared/components/character-badge/character-badge.component';
import { NavbuttonComponent } from './navbutton/navbutton.component';
import { NgFor, NgIf } from '@angular/common';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { Subscription } from 'rxjs';
import { CharacterDto } from '../../../shared/models/Dtos/characterDto';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CharacterBadgeComponent, NavbuttonComponent, NgIf, NgFor],
  templateUrl: './navbar.component.html',
})
export class NavbarComponent implements OnInit, OnDestroy {
  @Output() itemTapped = new EventEmitter<void>();
  @Input() isScreenSmall!: boolean;
  showList = false;
  activeLabel = 'Character'; // Default active label
  navButtons = [
    { link: '/game/character', label: 'Character' },
    { link: '/game/city', label: 'City' },
    { link: '/game/professions', label: 'Professions' },
    { link: '/game/world', label: 'World' },
    // { link: '#', label: 'Town' },
    { link: '/game/settings', label: 'Settings' },
  ];

  currentCharacter: CharacterDto | null = null;
  private subscription: Subscription = new Subscription();

  constructor(private authService: AuthService) {}

  ngOnInit() {
    this.subscription.add(
      this.authService.currentCharacter$.subscribe((character) => {
        this.currentCharacter = character;
      }),
    );
  }

  ngOnDestroy() {
    this.subscription.unsubscribe();
  }

  toggleList() {
    this.showList = !this.showList;
  }

  activeNavbar(activeLabel: string) {
    this.activeLabel = activeLabel;
    this.itemTapped.emit();
  }

  logout() {
    this.authService.logout();
  }
}
