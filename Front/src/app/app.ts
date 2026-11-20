import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatToolbarModule } from '@angular/material/toolbar';
import { CommentDto } from './models/comment.model';
import { CommentsService } from './services/comments.service';

@Component({
  selector: 'app-root',
  imports: [DatePipe, MatCardModule, MatToolbarModule],
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
