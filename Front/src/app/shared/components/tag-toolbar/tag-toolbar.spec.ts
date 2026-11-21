import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TagToolbar } from './tag-toolbar';

describe('TagToolbar', () => {
  let component: TagToolbar;
  let fixture: ComponentFixture<TagToolbar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TagToolbar],
    }).compileComponents();

    fixture = TestBed.createComponent(TagToolbar);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
