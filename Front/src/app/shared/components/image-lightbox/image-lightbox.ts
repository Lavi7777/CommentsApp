import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';

export interface ImageLightboxData {
  url: string;
  title: string;
}

@Component({
  selector: 'app-image-lightbox',
  imports: [MatDialogModule, MatButtonModule],
  templateUrl: './image-lightbox.html',
})
export class ImageLightbox {
  protected readonly data = inject<ImageLightboxData>(MAT_DIALOG_DATA);
}
