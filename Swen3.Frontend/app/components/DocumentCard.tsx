import styles from '../dashboard/page.module.css';
import { useState, useEffect, useRef } from 'react';
import { buildApiUrl } from '../utils/utils';
import { Document } from '../models/Document';
import SummaryPopup from './SummaryPopup';

const geminiEndpoint = 'http://localhost:8090/api/Gemini';
const documentsEndpoint = buildApiUrl('/api/Documents');

export default function DocumentCard(doc: Document) {

  const [error, setError] = useState<string | null>(null);
  const [showSummaryPopup, setShowSummaryPopup] = useState(false);
  const [summaryContent, setSummaryContent] = useState<string | null>(null);
  const [summarizing, setSummarizing] = useState(false);
  const [currentSummaryDocTitle, setCurrentSummaryDocTitle] = useState<string | null>(null);
  const [documents, setDocuments] = useState<Document[]>([]);

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

  const handleSummarize = async () => {
    try {
      setSummarizing(true);
      setShowSummaryPopup(true); // Open popup immediately to show "Generating..."
      setError(null);

      const response = await fetch(`${geminiEndpoint}/summarize`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(doc.metadata), // Ensure your backend expects this format
      });

      if (!response.ok) throw new Error('Summarization failed');

      const result = await response.json();
      setSummaryContent(result.candidates[0].content.parts[0].text || 'No summary available.');
    } catch (err) {
      setSummaryContent(err instanceof Error ? err.message : 'Error fetching summary');
    } finally {
      setSummarizing(false);
    }
  };

  return (
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
            title={doc.title}
            content={summaryContent}
            isSummarizing={summarizing}
            onClose={() => setShowSummaryPopup(false)}
          />
        )}
      </div>
    </div>
  );
}
