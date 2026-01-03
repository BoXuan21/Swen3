export interface Document {
  id: string;
  title: string;
  fileName: string;
  mimeType: string;
  size: number;
  uploadedAt: string;
  metadata: string;
  storageKey: string;
}
