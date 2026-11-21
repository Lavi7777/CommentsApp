import { Component, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { FormatTag } from '../../utils/wrap-selection';

@Component({
  selector: 'app-tag-toolbar',
  imports: [MatButtonModule],
  templateUrl: './tag-toolbar.html',
})
export class TagToolbar {
  readonly tagSelected = output<FormatTag>();

  protected readonly tags: FormatTag[] = ['i', 'strong', 'code', 'a'];

  protected select(tag: FormatTag): void {
    this.tagSelected.emit(tag);
  }
}
