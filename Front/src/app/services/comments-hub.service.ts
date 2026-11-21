import { Injectable, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

export interface CommentCreatedMessage {
  id: string;
  parentId: string | null;
}

@Injectable({ providedIn: 'root' })
export class CommentsHubService implements OnDestroy {
  private readonly connection: HubConnection = new HubConnectionBuilder()
    .withUrl('/hubs/comments')
    .withAutomaticReconnect()
    .build();

  private readonly commentCreatedSubject = new Subject<CommentCreatedMessage>();

  readonly commentCreated$: Observable<CommentCreatedMessage> =
    this.commentCreatedSubject.asObservable();

  constructor() {
    this.connection.on('commentCreated', (message: CommentCreatedMessage) =>
      this.commentCreatedSubject.next(message),
    );
  }

  start(): void {
    if (this.connection.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.connection.start().catch(() => setTimeout(() => this.start(), 5000));
  }

  ngOnDestroy(): void {
    void this.connection.stop();
  }
}
