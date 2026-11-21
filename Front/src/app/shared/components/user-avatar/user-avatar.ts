import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-user-avatar',
  templateUrl: './user-avatar.html',
})
export class UserAvatar {
  readonly name = input.required<string>();

  protected readonly letter = computed(() => this.name().charAt(0).toUpperCase());
}
