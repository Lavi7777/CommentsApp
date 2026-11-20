import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CommentDto, PagedResult } from '../models/comment.model';

@Injectable({ providedIn: 'root' })
export class CommentsService {
  private readonly http = inject(HttpClient);

  getList(page: number, sortBy: string, desc: boolean): Observable<PagedResult<CommentDto>> {
    return this.http.get<PagedResult<CommentDto>>('/api/comments', {
      params: { page, sortBy, desc },
    });
  }
}
