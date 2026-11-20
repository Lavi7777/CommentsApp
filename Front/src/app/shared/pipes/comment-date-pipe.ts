import { DatePipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'commentDate' })
export class CommentDatePipe implements PipeTransform {
  private readonly datePipe = new DatePipe('en-US');

  transform(value: string | Date): string {
    const date = this.datePipe.transform(value, 'dd.MM.yy');
    const time = this.datePipe.transform(value, 'HH:mm');
    return `${date} в ${time}`;
  }
}
