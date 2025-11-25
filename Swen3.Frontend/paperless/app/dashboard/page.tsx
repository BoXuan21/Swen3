'use client';

import { useState, useEffect } from 'react';
import styles from './page.module.css';

interface Document {
  id: string;
  title: string;
  fileName: string;
  mimeType: string;
  size: number;
  uploadedAt: string;
  metadata: string;
}

export default function Dashboard() {
  const [documents, setDocuments] = useState<Document[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showAddForm, setShowAddForm] = useState(false);
  const [newDocument, setNewDocument] = useState({
    title: '',
    fileName: '',
    mimeType: '',
    metadata: ''
  });

  // Fetch documents from API
  const fetchDocuments = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await fetch('/api/Documents');
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

  // Add new document
  const addDocument = async () => {
    try {
      const response = await fetch('/api/Documents', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(newDocument)
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const addedDoc = await response.json();
      setDocuments([addedDoc, ...documents]);
      setShowAddForm(false);
      setNewDocument({ title: '', fileName: '', mimeType: '', metadata: '' });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add document');
    }
  };

  // Delete document
  const deleteDocument = async (id: string) => {
    try {
      const response = await fetch(`/api/Documents/${id}`, {
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

  useEffect(() => {
    fetchDocuments();
  }, []);

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
            onClick={() => setShowAddForm(!showAddForm)}
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

        {/* Add Document Form */}
        {showAddForm && (
          <div className={styles.formContainer}>
            <h3 className={styles.formTitle}>Add New Document</h3>
            <div className={styles.formGrid}>
              <input
                type="text"
                placeholder="Document Title"
                value={newDocument.title}
                onChange={(e) => setNewDocument({...newDocument, title: e.target.value})}
                className={styles.input}
              />
              <input
                type="text"
                placeholder="File Name"
                value={newDocument.fileName}
                onChange={(e) => setNewDocument({...newDocument, fileName: e.target.value})}
                className={styles.input}
              />
              <input
                type="text"
                placeholder="MIME Type (e.g., application/pdf)"
                value={newDocument.mimeType}
                onChange={(e) => setNewDocument({...newDocument, mimeType: e.target.value})}
                className={styles.input}
              />
            </div>
            <div className={styles.formActions}>
              <button
                onClick={addDocument}
                className={styles.buttonPrimary}
              >
                Save Document
              </button>
              <button
                onClick={() => setShowAddForm(false)}
                className={styles.buttonSecondary}
              >
                Cancel
              </button>
            </div>
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
                </div>
                <div className={styles.actions}>
                  <button
                    onClick={() => {
                      alert(`Document Details:\n\nTitle: ${doc.title}\nFile: ${doc.fileName}\nType: ${doc.mimeType}\nUploaded: ${new Date(doc.uploadedAt).toLocaleString()}`);
                    }}
                    className={styles.btnView}
                  >
                    View Details
                  </button>
                  <button
                    onClick={() => deleteDocument(doc.id)}
                    className={styles.btnDelete}
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
