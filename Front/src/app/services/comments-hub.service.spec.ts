import { TestBed } from '@angular/core/testing';
import { CommentsHubService } from './comments-hub.service';

describe('CommentsHubService', () => {
  let service: CommentsHubService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CommentsHubService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
