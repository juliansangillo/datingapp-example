import { Component, Input, EventEmitter, OnInit, Output } from '@angular/core';
import { Photo } from 'src/app/_models/photo';

@Component({
    selector: 'app-photo-approval-card',
    templateUrl: './photo-approval-card.component.html',
    styleUrls: ['./photo-approval-card.component.css']
})
export class PhotoApprovalCardComponent implements OnInit {
    @Input() photo: Photo;

    @Output() approve: EventEmitter<number> = new EventEmitter();
    @Output() reject: EventEmitter<number> = new EventEmitter();

    constructor() { }

    ngOnInit(): void {
    }

    onApprove() {
        this.approve.emit(this.photo.id);
    }

    onReject() {
        this.reject.emit(this.photo.id);
    }
}
