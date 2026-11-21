import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';

export interface SortOption {
  key: string;
  label: string;
}

export interface SortState {
  sortBy: string;
  desc: boolean;
}

@Component({
  selector: 'app-sort-header',
  imports: [MatButtonModule],
  templateUrl: './sort-header.html',
})
export class SortHeader {
  readonly options = input.required<SortOption[]>();
  readonly sortBy = input.required<string>();
  readonly desc = input.required<boolean>();
  readonly changed = output<SortState>();

  protected select(key: string): void {
    if (key === this.sortBy()) {
      this.changed.emit({ sortBy: key, desc: !this.desc() });
      return;
    }

    this.changed.emit({ sortBy: key, desc: key === 'date' });
  }
}
