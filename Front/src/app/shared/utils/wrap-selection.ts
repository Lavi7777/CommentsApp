export type FormatTag = 'i' | 'strong' | 'code' | 'a';

export interface WrapResult {
  value: string;
  selectionStart: number;
  selectionEnd: number;
}

export function wrapSelection(
  value: string,
  start: number,
  end: number,
  tag: FormatTag,
): WrapResult {
  const selected = value.slice(start, end);
  const open = tag === 'a' ? '<a href="https://" title="">' : `<${tag}>`;
  const close = `</${tag}>`;
  const result = value.slice(0, start) + open + selected + close + value.slice(end);

  if (tag === 'a') {
    const hrefStart = start + '<a href="'.length;
    return {
      value: result,
      selectionStart: hrefStart,
      selectionEnd: hrefStart + 'https://'.length,
    };
  }

  const innerStart = start + open.length;
  return {
    value: result,
    selectionStart: innerStart,
    selectionEnd: innerStart + selected.length,
  };
}
