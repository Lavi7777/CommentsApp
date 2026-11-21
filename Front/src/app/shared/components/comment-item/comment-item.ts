import { Component, inject, input, output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { AttachmentDto, CommentDto } from '../../../models/comment.model';
import { ImageLightbox } from '../image-lightbox/image-lightbox';
import { UserAvatar } from '../user-avatar/user-avatar';
import { CommentDatePipe } from '../../pipes/comment-date-pipe';
import { FileSizePipe } from '../../pipes/file-size-pipe';
import { CommentForm } from '../../../components/comment-form/comment-form';

@Component({
  selector: 'app-comment-item',
  imports: [MatButtonModule, CommentForm, UserAvatar, CommentDatePipe, FileSizePipe],
  templateUrl: './comment-item.html',
  host: { class: 'block' },
})
export class CommentItem {
  private readonly dialog = inject(MatDialog);

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

  protected openImage(attachment: AttachmentDto): void {
    this.dialog.open(ImageLightbox, {
      data: { url: attachment.url, title: attachment.originalFileName },
      maxWidth: '95vw',
      autoFocus: false,
    });
  }
}
