export interface Document {
  id: string;
  title: string;
  fileName: string;
  mimeType: string;
  size: number;
  uploadedAt: string;
  metadata: string;
  storageKey: string;
  priorityId: number | null;
  priorityName: string | null;
}

export interface Priority {
  id: number;
  name: string;
  level: number;
}
