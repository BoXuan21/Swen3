'use client';

import { createPortal } from "react-dom";
import { useEffect, useState } from "react";
import styles from "../dashboard/page.module.css";
import { Document } from "../models/Document";

interface SummaryPopupProps {
  document: Document;
  content: string | null; // This is the content passed from DocumentCard
  isSummarizing: boolean;
  onClose: () => void;
}

export default function SummaryPopup({ document: doc, content, isSummarizing, onClose }: SummaryPopupProps) {
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    document.body.style.overflow = 'hidden';
    return () => { document.body.style.overflow = 'unset'; };
  }, []);

  if (!mounted) return null;

  const hasSummary = doc.summary !== "" && doc.summary !== null;

  return createPortal(
    <div className={styles.modalOverlay} onClick={onClose}>
      <div className={styles.modalContent} onClick={(e) => e.stopPropagation()}>
        <div className={styles.modalHeader}>
          <h2 className={styles.modalTitle}>
            Summary for: <strong>{doc.title}</strong>
          </h2>
          <button onClick={onClose} className={styles.modalCloseButton}>
            &times;
          </button>
        </div>

        <div className={styles.modalBody}>
          {isSummarizing ? (
            <div className={styles.loading}>Generating summary...</div>
          ) : content ? (
            <p className={styles.summaryText}>{content}</p>
          ) : (
            <p>No summary available.</p>
          )}
        </div>

        <div className={styles.modalFooter}>
          <button onClick={onClose} className={styles.buttonSecondary}>
            Close
          </button>
        </div>
      </div>
    </div>,
    document.body
  );
}
