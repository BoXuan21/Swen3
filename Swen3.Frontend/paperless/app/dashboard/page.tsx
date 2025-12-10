'use client';

import { useState, useEffect, useRef } from 'react';
import styles from './page.module.css';

interface Document {
  id: string;
  title: string;
  fileName: string;
  mimeType: string;
  size: number;
  uploadedAt: string;
  metadata: string;
  storageKey: string;
}

const MAX_FILE_BYTES = 25 * 1024 * 1024; // 25 MB
const MAX_FILE_MB = MAX_FILE_BYTES / (1024 * 1024);
const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? '';

const buildApiUrl = (path: string) => `${API_BASE}${path}`;

export default function Dashboard() {
  const [documents, setDocuments] = useState<Document[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showUploadForm, setShowUploadForm] = useState(false);
  const [title, setTitle] = useState('');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadMessage, setUploadMessage] = useState<string | null>(null);
  const [dragActive, setDragActive] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [previewDocId, setPreviewDocId] = useState<string | null>(null);

  const [showSummaryPopup, setShowSummaryPopup] = useState(false);
  const [summaryContent, setSummaryContent] = useState<string | null>(null);
  const [summarizing, setSummarizing] = useState(false);
  const [currentSummaryDocTitle, setCurrentSummaryDocTitle] = useState<string | null>(null);

  const documentsEndpoint = buildApiUrl('/api/Documents');
  const geminiEndpoint = buildApiUrl('http://localhost:8090/api/Gemini');

  // Fetch documents from API
  const fetchDocuments = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await fetch(documentsEndpoint);
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      const data = await response.json();
      setDocuments(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch documents');
    } finally {
      setLoading(false);
    }
  };

  const resetUploadState = () => {
    setTitle('');
    setSelectedFile(null);
    setUploadMessage(null);
    setDragActive(false);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
    }
    setPreviewUrl(null);
  };

  // Add new document
  const uploadDocument = async () => {
    try {
      setUploadMessage(null);
      setError(null);

      if (!title.trim()) {
        throw new Error('Please provide a document title.');
      }

      if (!selectedFile) {
        throw new Error('Please drop a PDF file.');
      }

      if (selectedFile.size > MAX_FILE_BYTES) {
        throw new Error(`File exceeds ${MAX_FILE_MB} MB limit.`);
      }

      const formData = new FormData();
      formData.append('title', title.trim());
      formData.append('file', selectedFile);

      setUploading(true);

      const response = await fetch(documentsEndpoint, {
        method: 'POST',
        body: formData
      });

      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || `HTTP error! status: ${response.status}`);
      }

      const addedDoc = await response.json();
      setDocuments((prev) => [addedDoc, ...prev]);
      resetUploadState();
      setUploadMessage('Document uploaded successfully.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to upload document');
    } finally {
      setUploading(false);
    }
  };

  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  // Delete document
  const deleteDocument = async (id: string) => {
    try {
      const response = await fetch(`${documentsEndpoint}/${id}`, {
        method: 'DELETE'
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      setDocuments(documents.filter(doc => doc.id !== id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete document');
    }
  };

  const summarize = async (doc: Document) => {
    try {
      setSummarizing(true);
      setSummaryContent(null);
      setShowSummaryPopup(true);
      setCurrentSummaryDocTitle(doc.title);
      setError(null);

      // Using the document ID in the request body
      const response = await fetch(`${geminiEndpoint}/summarize`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(doc.metadata),
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || `HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      setSummaryContent(result.summaryText || 'No summary available.');

    } catch (err) {
      setSummaryContent(`Error: ${err instanceof Error ? err.message : 'Failed to fetch summary'}`);
      setError(err instanceof Error ? err.message : 'Failed to get document summary');
    } finally {
      setSummarizing(false);
    }
  }

  const closeSummaryPopup = () => {
    setShowSummaryPopup(false);
    setSummaryContent(null);
    setCurrentSummaryDocTitle(null);
  };

  useEffect(() => {
    fetchDocuments();
  }, []);

  useEffect(() => {
    if (showUploadForm) {
      setUploadMessage(null);
    }
  }, [showUploadForm]);

  const handleFileSelection = (file: File | null) => {
    if (!file) return;

    if (file.type !== 'application/pdf' && !file.name.toLowerCase().endsWith('.pdf')) {
      setError('Only PDF files are supported.');
      return;
    }

    if (file.size > MAX_FILE_BYTES) {
      setError(`File exceeds ${MAX_FILE_MB} MB limit.`);
      return;
    }

    setError(null);
    setSelectedFile(file);
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
    }
    setPreviewUrl(URL.createObjectURL(file));
  };

  const onDrop = (event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setDragActive(false);
    const file = event.dataTransfer.files?.[0];
    handleFileSelection(file ?? null);
  };

  const fileSizeUsage = selectedFile ? (selectedFile.size / MAX_FILE_BYTES) * 100 : 0;

  const handleDownload = async (id: string) => {
    try {
      const doc = documents.find(d => d.id === id);
      const downloadUrl = buildApiUrl(`/api/Documents/${id}/content`);

      const response = await fetch(downloadUrl);
      if (!response.ok) throw new Error('Download failed');

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = doc?.fileName || 'document.pdf';
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to download document');
    }
  };

  const handlePreview = (id: string) => {
    console.log('Opening preview for document:', id);
    setPreviewDocId(id);
  };

  const closePreview = () => {
    console.log('Closing preview');
    setPreviewDocId(null);
  };

  return (
    <div className={styles.root}>
      <div className={styles.container}>
        {/* Header */}
        <div className={styles.header}>
          <h1 className={styles.title}>
            📊 Document Dashboard
          </h1>
          <p className={styles.subtitle}>Manage your documents efficiently</p>
        </div>

        {/* Stats Cards */}
        <div className={styles.statsGrid}>
          <div className={styles.card}>
            <div className={`${styles.metric} ${styles.metricBlue}`}>{documents.length}</div>
            <div className={styles.subtitle}>Total Documents</div>
          </div>
        </div>

        {/* Controls */}
        <div className={styles.controls}>
          <button
            onClick={fetchDocuments}
            className={styles.buttonPrimary}
          >
            🔄 Refresh
          </button>
          <button
            onClick={() => setShowUploadForm(!showUploadForm)}
            className={styles.buttonSuccess}
          >
            ➕ Add Document
          </button>
        </div>

        {/* Error Message */}
        {error && (
          <div className={styles.errorBox}>
            Error: {error}
          </div>
        )}

        {/* Add Document Form (omitted for brevity) */}
        {showUploadForm && (
          <div className={styles.formContainer}>
            <h3 className={styles.formTitle}>Upload PDF (Max {MAX_FILE_MB} MB)</h3>
            <div className={styles.formGrid}>
              <input
                type="text"
                placeholder="Document Title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                className={styles.input}
              />
            </div>

            <div
              className={`${styles.dropZone} ${dragActive ? styles.dropZoneActive : ''}`}
              onDragOver={(e) => {
                e.preventDefault();
                setDragActive(true);
              }}
              onDragLeave={(e) => {
                e.preventDefault();
                setDragActive(false);
              }}
              onDrop={onDrop}
              onClick={() => fileInputRef.current?.click()}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  fileInputRef.current?.click();
                }
              }}
            >
              <p className={styles.dropHint}>
                Drag and drop your PDF here, or click to browse
              </p>
              <p className={styles.dropSubHint}>
                Accepted format: PDF · Maximum size {MAX_FILE_MB} MB
              </p>
              {selectedFile && (
                <div className={styles.fileInfo}>
                  <span>{selectedFile.name}</span>
                  <span>{(selectedFile.size / (1024 * 1024)).toFixed(2)} MB</span>
                </div>
              )}

              <div className={styles.sizeMeter}>
                <div
                  className={styles.sizeMeterFill}
                  style={{ width: `${Math.min(fileSizeUsage, 100)}%` }}
                />
              </div>
            </div>
            <input
              ref={fileInputRef}
              type="file"
              accept="application/pdf"
              className={styles.hiddenInput}
              onChange={(e) => handleFileSelection(e.target.files?.[0] ?? null)}
            />

            {previewUrl ? (
              <div className={styles.previewContainer}>
                <p className={styles.previewLabel}>Preview</p>
                <iframe
                  src={previewUrl}
                  className={styles.previewFrame}
                  title="PDF preview"
                />
              </div>
            ) : (
              <div className={styles.previewPlaceholder}>
                Drop a PDF to see a preview.
              </div>
            )}

            <div className={styles.formActions}>
              <button
                onClick={uploadDocument}
                className={styles.buttonPrimary}
                disabled={uploading}
              >
                {uploading ? 'Uploading…' : 'Upload Document'}
              </button>
              <button
                onClick={() => {
                  setShowUploadForm(false);
                  resetUploadState();
                }}
                className={styles.buttonSecondary}
                disabled={uploading}
              >
                Cancel
              </button>
            </div>

            {uploadMessage && (
              <div className={styles.successBox}>{uploadMessage}</div>
            )}
          </div>
        )}

        {/* Documents Grid */}
        {loading ? (
          <div className={styles.loading}>
            <div className={styles.loadingText}>Loading documents...</div>
          </div>
        ) : documents.length === 0 ? (
          <div className={styles.empty}>
            <div className={styles.emptyPrimary}>📭 No documents found</div>
            <div className={styles.emptySecondary}>Start by adding your first document!</div>
          </div>
        ) : (
          <div className={styles.documentsGrid}>
            {documents.map((doc) => (
              <div key={doc.id} className={styles.docCard}>
                <h3 className={styles.docTitle}>{doc.title}</h3>
                <div className={styles.docMeta}>
                  <div>📁 {doc.fileName}</div>
                  <div>📅 {new Date(doc.uploadedAt).toLocaleDateString()}</div>
                  <div>🏷️ {doc.mimeType}</div>
                  <div className={styles.sizePill}>
                    {(doc.size / (1024 * 1024)).toFixed(2)} MB
                  </div>
                </div>

                {/* Metadata / OCR Text Section */}
                <div className={styles.metadataSection}>
                  <div className={styles.metadataLabel}>
                    📝 OCR Text
                  </div>
                  {doc.metadata && doc.metadata.trim() !== '' ? (
                    <div className={styles.metadataContent}>
                      {doc.metadata}
                    </div>
                  ) : (
                    <div className={styles.metadataEmpty}>
                      Kein OCR-Text verfügbar
                    </div>
                  )}
                </div>

                {/* Document Actions */}
                <div className={styles.docActions}>
                  <button
                    onClick={() => summarize(doc)}
                    className={styles.btnSummarize}
                    disabled={summarizing}
                  >
                    📝 Summarize
                  </button>
                  <button
                    onClick={() => handleDownload(doc.id)}
                    className={styles.btnDownload}
                  >
                    ⬇️ Download
                  </button>
                  <button
                    onClick={() => deleteDocument(doc.id)}
                    className={styles.btnDelete}
                  >
                    🗑️ Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Summary Popup / Modal */}
      {showSummaryPopup && (
        <div className={styles.modalOverlay}>
          <div className={styles.modalContent}>
            <div className={styles.modalHeader}>
              <h2 className={styles.modalTitle}>
                Summary for: **{currentSummaryDocTitle}**
              </h2>
              <button onClick={closeSummaryPopup} className={styles.modalCloseButton}>
                &times;
              </button>
            </div>

            <div className={styles.modalBody}>
              {summarizing ? (
                <div className={styles.loading}>Generating summary...</div>
              ) : summaryContent ? (
                <p className={styles.summaryText}>{summaryContent}</p>
              ) : (
                <p>Failed to load summary or no summary available.</p>
              )}
            </div>

            <div className={styles.modalFooter}>
              <button onClick={closeSummaryPopup} className={styles.buttonSecondary}>
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
