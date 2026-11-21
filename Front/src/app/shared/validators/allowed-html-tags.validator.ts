import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const TAG_REGEX = /<[^<>]*>/g;
const SIMPLE_OPEN = /^<(i|strong|code)>$/i;
const CLOSE = /^<\/(i|strong|code|a)>$/i;
const ANCHOR_OPEN = /^<a\s+href="([^"<>]*)"(?:\s+title="([^"<>]*)")?\s*>$/i;

export function allowedHtmlTagsValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = typeof control.value === 'string' ? control.value : '';
    const stack: string[] = [];

    for (const match of value.matchAll(TAG_REGEX)) {
      const tag = match[0];

      const simple = SIMPLE_OPEN.exec(tag);
      if (simple) {
        stack.push(simple[1].toLowerCase());
        continue;
      }

      const anchor = ANCHOR_OPEN.exec(tag);
      if (anchor) {
        if (!isHttpUrl(anchor[1])) {
          return { htmlTags: 'Link must be a valid http or https URL.' };
        }
        stack.push('a');
        continue;
      }

      const close = CLOSE.exec(tag);
      if (close) {
        const name = close[1].toLowerCase();
        if (stack.pop() !== name) {
          return { htmlTags: `Tag </${name}> is closed in the wrong place.` };
        }
        continue;
      }

      return { htmlTags: `Tag ${tag} is not allowed. Allowed: a, code, i, strong.` };
    }

    if (stack.length > 0) {
      return { htmlTags: `Tag <${stack[stack.length - 1]}> is not closed.` };
    }

    return null;
  };
}

function isHttpUrl(value: string): boolean {
  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:';
  } catch {
    return false;
  }
}
