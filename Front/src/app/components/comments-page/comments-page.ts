import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatToolbarModule } from '@angular/material/toolbar';
import { CommentDto } from '../../models/comment.model';
import { CommentsHubService } from '../../services/comments-hub.service';
import { CommentsService } from '../../services/comments.service';
import { SortHeader, SortOption, SortState } from '../../shared/components/sort-header/sort-header';
import { CommentForm } from '../comment-form/comment-form';
import { CommentItem } from '../../shared/components/comment-item/comment-item';

@Component({
  selector: 'app-comments-page',
  imports: [MatToolbarModule, MatPaginatorModule, CommentForm, CommentItem, SortHeader],
  templateUrl: './comments-page.html',
})
export class CommentsPage {
  private readonly commentsService = inject(CommentsService);
  private readonly hub = inject(CommentsHubService);

  protected readonly pageSize = 25;
  protected readonly sortOptions: SortOption[] = [
    { key: 'username', label: 'User Name' },
    { key: 'email', label: 'E-mail' },
    { key: 'date', label: 'Date' },
  ];

  protected readonly comments = signal<CommentDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly error = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly sortBy = signal('date');
  protected readonly desc = signal(true);

  constructor() {
    this.load();
    this.hub.start();
    this.hub.commentCreated$.pipe(takeUntilDestroyed()).subscribe(() => this.load());
    this.hub.reconnected$.pipe(takeUntilDestroyed()).subscribe(() => this.load());
  }

  protected onCreated(): void {
    this.load();
  }

  protected onSortChanged(state: SortState): void {
    this.sortBy.set(state.sortBy);
    this.desc.set(state.desc);
    this.page.set(1);
    this.load();
  }

  protected onPage(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.load();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  private load(): void {
    this.commentsService.getList(this.page(), this.sortBy(), this.desc()).subscribe({
      next: (result) => {
        this.comments.set(result.items);
        this.totalCount.set(result.totalCount);
        this.error.set(null);
      },
      error: () => this.error.set('Failed to load comments.'),
    });
  }
}
