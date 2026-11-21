import { Component } from '@angular/core';
import { CommentsPage } from './components/comments-page/comments-page';

@Component({
  selector: 'app-root',
  imports: [CommentsPage],
  templateUrl: './app.html',
})
export class App {}
