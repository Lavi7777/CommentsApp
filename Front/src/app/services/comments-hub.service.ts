import { Injectable, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

export interface CommentCreatedMessage {
  id: string;
  parentId: string | null;
}

const RETRY_DELAY_MS = 5000;

@Injectable({ providedIn: 'root' })
export class CommentsHubService implements OnDestroy {
  private readonly connection: HubConnection = new HubConnectionBuilder()
    .withUrl('/hubs/comments')
    .withAutomaticReconnect()
    .build();

  private readonly commentCreatedSubject = new Subject<CommentCreatedMessage>();
  private readonly reconnectedSubject = new Subject<void>();

  private wasConnected = false;
  private stopped = false;

  readonly commentCreated$: Observable<CommentCreatedMessage> =
    this.commentCreatedSubject.asObservable();

  readonly reconnected$: Observable<void> = this.reconnectedSubject.asObservable();

  constructor() {
    this.connection.on('commentCreated', (message: CommentCreatedMessage) =>
      this.commentCreatedSubject.next(message),
    );
    this.connection.onreconnected(() => this.reconnectedSubject.next());
    this.connection.onclose(() => this.scheduleStart());
  }

  start(): void {
    if (this.connection.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.connection
      .start()
      .then(() => {
        if (this.wasConnected) {
          this.reconnectedSubject.next();
        }
        this.wasConnected = true;
      })
      .catch(() => this.scheduleStart());
  }

  ngOnDestroy(): void {
    this.stopped = true;
    void this.connection.stop();
  }

  private scheduleStart(): void {
    if (this.stopped) {
      return;
    }

    setTimeout(() => this.start(), RETRY_DELAY_MS);
  }
}
