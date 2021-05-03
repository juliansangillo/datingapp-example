import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PhotoApprovalCardComponent } from './photo-approval-card.component';

describe('PhotoApprovalCardComponent', () => {
  let component: PhotoApprovalCardComponent;
  let fixture: ComponentFixture<PhotoApprovalCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ PhotoApprovalCardComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(PhotoApprovalCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
