import styles from '../dashboard/page.module.css';
import { useState } from 'react';
import { buildApiUrl } from '../utils/utils';
import { Document, Priority } from '../models/Document';
import SummaryPopup from './SummaryPopup';

const geminiEndpoint = buildApiUrl('/api/Gemini');
const documentsEndpoint = buildApiUrl('/api/Documents');

interface DocumentCardProps {
  document: Document;
  priorities: Priority[];
  onDocumentUpdate: (doc: Document) => void;
}

export default function DocumentCard({ document: doc, priorities, onDocumentUpdate }: DocumentCardProps) {
  const [error, setError] = useState<string | null>(null);
  const [showSummaryPopup, setShowSummaryPopup] = useState(false);
  const [summaryContent, setSummaryContent] = useState<string | null>(null);
  const [summarizing, setSummarizing] = useState(false);
  const [updatingPriority, setUpdatingPriority] = useState(false);

  const handleDownload = async (id: string) => {
    try {
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

  const deleteDocument = async (id: string) => {
    try {
      const response = await fetch(`${documentsEndpoint}/${id}`, {
        method: 'DELETE'
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      // Trigger a refresh by calling onDocumentUpdate with a deleted flag
      // For now, we'll just reload the page
      window.location.reload();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete document');
    }
  };

  const handlePriorityChange = async (newPriorityId: number | null) => {
    try {
      setUpdatingPriority(true);
      setError(null);

      const response = await fetch(`${documentsEndpoint}/${doc.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          ...doc,
          priorityId: newPriorityId
        })
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const updatedDoc = await response.json();
      onDocumentUpdate(updatedDoc);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update priority');
    } finally {
      setUpdatingPriority(false);
    }
  };

  const handleSummarize = async () => {
    if (doc.summary && doc.summary.trim() !== '') {
      setSummaryContent(doc.summary);
      setShowSummaryPopup(true);
      return;
    }

    try {
      setSummarizing(true);
      setShowSummaryPopup(true);
      setError(null);
      setSummaryContent(null);

      const payload = {
        documentId: doc.id,
        textToSummarize: doc.metadata
      };

      const response = await fetch(`${geminiEndpoint}/summarize`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      if (!response.ok) throw new Error('Summarization failed');

      const result = await response.text();

      setSummaryContent(result || 'No summary available.');

      onDocumentUpdate({
        ...doc,
        summary: result
      });

    } catch (err) {
      setSummaryContent(err instanceof Error ? err.message : 'Error fetching summary');
    } finally {
      setSummarizing(false);
    }
  };

  // Get priority color based on level
  const getPriorityColor = (priorityName: string | null) => {
    if (!priorityName) return styles.priorityNone;
    if (priorityName.includes('Very Important')) return styles.priorityHigh;
    if (priorityName.includes('Important')) return styles.priorityMedium;
    return styles.priorityLow;
  };

  return (
    <div key={doc.id} className={styles.docCard}>
      {/* Priority Badge */}
      <div className={styles.prioritySection}>
        <select
          value={doc.priorityId ?? ''}
          onChange={(e) => handlePriorityChange(e.target.value ? parseInt(e.target.value) : null)}
          className={`${styles.priorityBadge} ${getPriorityColor(doc.priorityName)}`}
          disabled={updatingPriority}
        >
          <option value="">No Priority</option>
          {priorities.map((priority) => (
            <option key={priority.id} value={priority.id}>
              {priority.name}
            </option>
          ))}
        </select>
        {updatingPriority && <span className={styles.priorityLoading}>⏳</span>}
      </div>

      <h3 className={styles.docTitle}>{doc.title}</h3>
      <div className={styles.docMeta}>
        <div>📁 {doc.fileName}</div>
        <div>📅 {new Date(doc.uploadedAt).toLocaleDateString()}</div>
        <div>🏷️ {doc.mimeType}</div>
        <div className={styles.sizePill}>
          {(doc.size / (1024 * 1024)).toFixed(2)} MB
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className={styles.cardError}>
          {error}
        </div>
      )}

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
            No preview text available
          </div>
        )}
      </div>

      {/* Document Actions */}
      <div className={styles.docActions}>
        <button
          onClick={() => handleSummarize()}
          className={styles.btnSummarize}
          disabled={summarizing}
        >
          {summarizing ? '⏳...' : '📝 Summarize'}
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
        {showSummaryPopup && (
          <SummaryPopup
            document={doc}
            content={summaryContent}
            isSummarizing={summarizing}
            onClose={() => setShowSummaryPopup(false)}
          />
        )}
      </div>
    </div>
  );
}
