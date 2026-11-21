import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, inject, input, output, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { CommentsService } from '../../services/comments.service';
import { TagToolbar } from '../../shared/components/tag-toolbar/tag-toolbar';
import { FormatTag, wrapSelection } from '../../shared/utils/wrap-selection';

const IMAGE_EXTENSIONS = ['jpg', 'jpeg', 'png', 'gif'];
const MAX_TEXT_FILE_BYTES = 100 * 1024;

@Component({
  selector: 'app-comment-form',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, TagToolbar],
  templateUrl: './comment-form.html',
  host: { class: 'block' },
})
export class CommentForm {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(CommentsService);
  private readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');
  private readonly textArea = viewChild<ElementRef<HTMLTextAreaElement>>('textArea');

  readonly parentId = input<string | null>(null);
  readonly created = output<void>();

  protected readonly captchaImage = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly fileError = signal<string | null>(null);
  protected readonly previewHtml = signal<string | null>(null);
  protected readonly previewError = signal<string | null>(null);
  protected readonly previewing = signal(false);

  private captchaId = '';
  private selectedFile: File | null = null;

  protected readonly form = this.fb.nonNullable.group({
    userName: [
      '',
      [Validators.required, Validators.maxLength(50), Validators.pattern(/^[a-zA-Z0-9]+$/)],
    ],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    homePage: ['', [Validators.pattern(/^https?:\/\/\S+$/), Validators.maxLength(2048)]],
    text: ['', [Validators.required, Validators.maxLength(5000)]],
    captchaCode: [
      '',
      [Validators.required, Validators.maxLength(10), Validators.pattern(/^[a-zA-Z0-9]+$/)],
    ],
  });

  constructor() {
    this.loadCaptcha();
  }

  protected loadCaptcha(): void {
    this.service.getCaptcha().subscribe({
      next: (captcha) => {
        this.captchaId = captcha.id;
        this.captchaImage.set(captcha.image);
      },
      error: () => this.serverError.set('Failed to load CAPTCHA.'),
    });
  }

  protected insertTag(tag: FormatTag): void {
    const element = this.textArea()?.nativeElement;
    if (!element) {
      return;
    }

    const result = wrapSelection(
      this.form.controls.text.value,
      element.selectionStart,
      element.selectionEnd,
      tag,
    );

    this.form.controls.text.setValue(result.value);
    element.focus();
    element.setSelectionRange(result.selectionStart, result.selectionEnd);
  }

  protected preview(): void {
    const text = this.form.controls.text.value;
    if (!text.trim()) {
      return;
    }

    this.previewing.set(true);
    this.previewError.set(null);

    this.service.preview(text).subscribe({
      next: (result) => {
        this.previewing.set(false);
        this.previewHtml.set(result.html);
      },
      error: (error: HttpErrorResponse) => {
        this.previewing.set(false);
        this.previewHtml.set(null);
        this.previewError.set(this.extractError(error));
      },
    });
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    this.selectedFile = null;
    this.fileError.set(null);

    if (!file) {
      return;
    }

    const extension = file.name.split('.').pop()?.toLowerCase() ?? '';

    if (extension === 'txt') {
      if (file.size > MAX_TEXT_FILE_BYTES) {
        this.rejectFile('Text file must not be larger than 100 KB.');
        return;
      }
    } else if (!IMAGE_EXTENSIONS.includes(extension)) {
      this.rejectFile('Allowed files: JPG, GIF, PNG and TXT.');
      return;
    }

    this.selectedFile = file;
  }

  protected submit(): void {
    this.serverError.set(null);

    if (this.form.invalid || this.fileError()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const data = new FormData();
    data.append('userName', value.userName.trim());
    data.append('email', value.email.trim());
    if (value.homePage.trim()) {
      data.append('homePage', value.homePage.trim());
    }
    data.append('text', value.text);
    data.append('captchaId', this.captchaId);
    data.append('captchaCode', value.captchaCode.trim());
    const parentId = this.parentId();
    if (parentId) {
      data.append('parentId', parentId);
    }
    if (this.selectedFile) {
      data.append('file', this.selectedFile);
    }

    this.submitting.set(true);

    this.service.create(data).subscribe({
      next: () => {
        this.submitting.set(false);
        this.form.controls.text.reset();
        this.form.controls.captchaCode.reset();
        this.previewHtml.set(null);
        this.previewError.set(null);
        this.clearFile();
        this.loadCaptcha();
        this.created.emit();
      },
      error: (error: HttpErrorResponse) => {
        this.submitting.set(false);
        this.serverError.set(this.extractError(error));
        this.form.controls.captchaCode.reset();
        this.loadCaptcha();
      },
    });
  }

  private rejectFile(message: string): void {
    this.fileError.set(message);
    this.clearFile();
  }

  private clearFile(): void {
    this.selectedFile = null;
    const element = this.fileInput();
    if (element) {
      element.nativeElement.value = '';
    }
  }

  private extractError(error: HttpErrorResponse): string {
    const body = error.error;

    if (body?.error) {
      return body.error;
    }

    if (body?.errors) {
      const first = Object.values(body.errors).flat()[0];
      if (typeof first === 'string') {
        return first;
      }
    }

    return 'Failed to send the comment.';
  }
}
