import { Component, input, output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { CommentDto } from '../../../models/comment.model';
import { UserAvatar } from '../user-avatar/user-avatar';
import { CommentDatePipe } from '../../pipes/comment-date-pipe';
import { CommentForm } from '../../../components/comment-form/comment-form';

@Component({
  selector: 'app-comment-item',
  imports: [MatButtonModule, CommentForm, UserAvatar, CommentDatePipe],
  templateUrl: './comment-item.html',
})
export class CommentItem {
  readonly comment = input.required<CommentDto>();
  readonly parentText = input<string | null>(null);
  readonly created = output<void>();

  protected readonly replying = signal(false);

  protected toggleReply(): void {
    this.replying.update((value) => !value);
  }

  protected onReplyCreated(): void {
    this.replying.set(false);
    this.created.emit();
  }
}
