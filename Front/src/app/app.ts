import { Component, inject, signal } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { CommentForm } from './components/comment-form/comment-form';
import { CommentItem } from './shared/components/comment-item/comment-item';
import { CommentDto } from './models/comment.model';
import { CommentsService } from './services/comments.service';

@Component({
  selector: 'app-root',
  imports: [MatToolbarModule, CommentForm, CommentItem],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly commentsService = inject(CommentsService);

  protected readonly comments = signal<CommentDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.load();
  }

  protected onCreated(): void {
    this.load();
  }

  private load(): void {
    this.commentsService.getList(1, 'date', true).subscribe({
      next: (result) => {
        this.comments.set(result.items);
        this.totalCount.set(result.totalCount);
        this.error.set(null);
      },
      error: () => this.error.set('Failed to load comments.'),
    });
  }
}
