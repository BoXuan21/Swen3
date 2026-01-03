'use client';

import { useState, useEffect, useRef } from 'react';
import styles from './page.module.css';
import { buildApiUrl } from '../utils/utils';
import { CONSTANTS } from '../utils/constants';
import { Document, Priority } from '../models/Document';
import DocumentCard from '../components/DocumentCard';

export default function Dashboard() {
  const [documents, setDocuments] = useState<Document[]>([]);
  const [priorities, setPriorities] = useState<Priority[]>([]);
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

  // Search and filter state
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedPriorityFilter, setSelectedPriorityFilter] = useState<number | null>(null);
  const [searching, setSearching] = useState(false);

  const [showSummaryPopup, setShowSummaryPopup] = useState(false);
  const [summaryContent, setSummaryContent] = useState<string | null>(null);
  const [summarizing, setSummarizing] = useState(false);
  const [currentSummaryDocTitle, setCurrentSummaryDocTitle] = useState<string | null>(null);

  const documentsEndpoint = buildApiUrl('/api/Documents');
  const prioritiesEndpoint = buildApiUrl('/api/Priorities');
  const geminiEndpoint = buildApiUrl('http://localhost:8090/api/Gemini');

  // Fetch priorities from API
  const fetchPriorities = async () => {
    try {
      const response = await fetch(prioritiesEndpoint);
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      const data = await response.json();
      setPriorities(data);
    } catch (err) {
      console.error('Failed to fetch priorities:', err);
    }
  };

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

  // Search documents
  const searchDocuments = async () => {
    try {
      setSearching(true);
      setError(null);

      const params = new URLSearchParams();
      if (searchQuery.trim()) {
        params.append('query', searchQuery.trim());
      }
      if (selectedPriorityFilter !== null) {
        params.append('priorityId', selectedPriorityFilter.toString());
      }

      const searchUrl = `${documentsEndpoint}/search?${params.toString()}`;
      const response = await fetch(searchUrl);
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      
      const data = await response.json();
      setDocuments(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to search documents');
    } finally {
      setSearching(false);
    }
  };

  // Clear search and show all documents
  const clearSearch = () => {
    setSearchQuery('');
    setSelectedPriorityFilter(null);
    fetchDocuments();
  };

  // Handle document update (for priority changes)
  const handleDocumentUpdate = (updatedDoc: Document) => {
    setDocuments(prevDocs => 
      prevDocs.map(doc => doc.id === updatedDoc.id ? updatedDoc : doc)
    );
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

      if (selectedFile.size > CONSTANTS.MAX_FILE_BYTES) {
        throw new Error(`File exceeds ${CONSTANTS.MAX_FILE_MB} MB limit.`);
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

  useEffect(() => {
    fetchDocuments();
    fetchPriorities();
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

    if (file.size > CONSTANTS.MAX_FILE_BYTES) {
      setError(`File exceeds ${CONSTANTS.MAX_FILE_MB} MB limit.`);
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

  const fileSizeUsage = selectedFile ? (selectedFile.size / CONSTANTS.MAX_FILE_BYTES) * 100 : 0;

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

        {/* Search and Filter Section */}
        <div className={styles.searchSection}>
          <div className={styles.searchBar}>
            <input
              type="text"
              placeholder="Search documents by content..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && searchDocuments()}
              className={styles.searchInput}
            />
            <select
              value={selectedPriorityFilter ?? ''}
              onChange={(e) => setSelectedPriorityFilter(e.target.value ? parseInt(e.target.value) : null)}
              className={styles.prioritySelect}
            >
              <option value="">All Priorities</option>
              {priorities.map((priority) => (
                <option key={priority.id} value={priority.id}>
                  {priority.name}
                </option>
              ))}
            </select>
            <button
              onClick={searchDocuments}
              className={styles.buttonSearch}
              disabled={searching}
            >
              {searching ? '⏳' : '🔍'} Search
            </button>
            {(searchQuery || selectedPriorityFilter !== null) && (
              <button
                onClick={clearSearch}
                className={styles.buttonClear}
              >
                ✕ Clear
              </button>
            )}
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
            <h3 className={styles.formTitle}>Upload PDF (Max {CONSTANTS.MAX_FILE_MB} MB)</h3>
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
                Accepted format: PDF · Maximum size {CONSTANTS.MAX_FILE_MB} MB
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
              <DocumentCard 
                key={doc.id}
                document={doc} 
                priorities={priorities}
                onDocumentUpdate={handleDocumentUpdate}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
