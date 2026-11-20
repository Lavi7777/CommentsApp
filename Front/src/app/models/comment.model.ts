export interface AttachmentDto {
  type: 'Image' | 'Text';
  originalFileName: string;
  url: string;
  sizeBytes: number;
}

export interface CommentDto {
  id: string;
  parentId: string | null;
  userName: string;
  email: string;
  homePage: string | null;
  text: string;
  createdAt: string;
  attachment: AttachmentDto | null;
  replies: CommentDto[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
