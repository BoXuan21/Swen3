'use client';

import { useState, useEffect } from 'react';

interface Document {
  id: string;
  title: string;
  fileName: string;
  mimeType: string;
  size: number;
  uploadedAt: string;
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
    size: 0
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
      setNewDocument({ title: '', fileName: '', mimeType: '', size: 0 });
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

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const totalSize = documents.reduce((sum, doc) => sum + doc.size, 0);

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="text-center mb-8">
          <h1 className="text-4xl font-bold text-gray-800 mb-2">
            📊 Document Dashboard
          </h1>
          <p className="text-gray-600">Manage your documents efficiently</p>
        </div>

        {/* Stats Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          <div className="bg-white rounded-lg shadow-md p-6">
            <div className="text-2xl font-bold text-blue-600">{documents.length}</div>
            <div className="text-gray-600">Total Documents</div>
          </div>
          <div className="bg-white rounded-lg shadow-md p-6">
            <div className="text-2xl font-bold text-green-600">{formatFileSize(totalSize)}</div>
            <div className="text-gray-600">Total Size</div>
          </div>
          <div className="bg-white rounded-lg shadow-md p-6">
            <div className="text-2xl font-bold text-purple-600">
              {documents.length > 0 ? Math.round(totalSize / documents.length) : 0} B
            </div>
            <div className="text-gray-600">Average Size</div>
          </div>
        </div>

        {/* Controls */}
        <div className="flex justify-between items-center mb-6">
          <button
            onClick={fetchDocuments}
            className="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-lg transition-colors"
          >
            🔄 Refresh
          </button>
          <button
            onClick={() => setShowAddForm(!showAddForm)}
            className="bg-green-500 hover:bg-green-600 text-white px-4 py-2 rounded-lg transition-colors"
          >
            ➕ Add Document
          </button>
        </div>

        {/* Error Message */}
        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-6">
            Error: {error}
          </div>
        )}

        {/* Add Document Form */}
        {showAddForm && (
          <div className="bg-white rounded-lg shadow-md p-6 mb-6">
            <h3 className="text-lg font-semibold mb-4">Add New Document</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <input
                type="text"
                placeholder="Document Title"
                value={newDocument.title}
                onChange={(e) => setNewDocument({...newDocument, title: e.target.value})}
                className="border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 text-black placeholder-gray-500"
              />
              <input
                type="text"
                placeholder="File Name"
                value={newDocument.fileName}
                onChange={(e) => setNewDocument({...newDocument, fileName: e.target.value})}
                className="border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 text-black placeholder-gray-500"
              />
              <input
                type="text"
                placeholder="MIME Type (e.g., application/pdf)"
                value={newDocument.mimeType}
                onChange={(e) => setNewDocument({...newDocument, mimeType: e.target.value})}
                className="border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 text-black placeholder-gray-500"
              />
              <input
                type="number"
                placeholder="File Size (bytes)"
                value={newDocument.size}
                onChange={(e) => setNewDocument({...newDocument, size: parseInt(e.target.value) || 0})}
                className="border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 text-black placeholder-gray-500"
              />
            </div>
            <div className="flex gap-2 mt-4">
              <button
                onClick={addDocument}
                className="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-lg transition-colors"
              >
                Save Document
              </button>
              <button
                onClick={() => setShowAddForm(false)}
                className="bg-gray-500 hover:bg-gray-600 text-white px-4 py-2 rounded-lg transition-colors"
              >
                Cancel
              </button>
            </div>
          </div>
        )}

        {/* Documents Grid */}
        {loading ? (
          <div className="text-center py-12">
            <div className="text-gray-600">Loading documents...</div>
          </div>
        ) : documents.length === 0 ? (
          <div className="text-center py-12">
            <div className="text-gray-600 text-lg">📭 No documents found</div>
            <div className="text-gray-500">Start by adding your first document!</div>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {documents.map((doc) => (
              <div key={doc.id} className="bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow">
                <h3 className="text-lg font-semibold text-gray-800 mb-2">{doc.title}</h3>
                <div className="space-y-2 text-sm text-gray-600">
                  <div>📁 {doc.fileName}</div>
                  <div>📅 {new Date(doc.uploadedAt).toLocaleDateString()}</div>
                  <div>🏷️ {doc.mimeType}</div>
                  <div className="bg-blue-100 text-blue-800 px-2 py-1 rounded inline-block">
                    {formatFileSize(doc.size)}
                  </div>
                </div>
                <div className="mt-4 flex gap-2">
                  <button
                    onClick={() => {
                      alert(`Document Details:\n\nTitle: ${doc.title}\nFile: ${doc.fileName}\nType: ${doc.mimeType}\nSize: ${formatFileSize(doc.size)}\nUploaded: ${new Date(doc.uploadedAt).toLocaleString()}`);
                    }}
                    className="bg-blue-500 hover:bg-blue-600 text-white px-3 py-1 rounded text-sm transition-colors"
                  >
                    View Details
                  </button>
                  <button
                    onClick={() => deleteDocument(doc.id)}
                    className="bg-red-500 hover:bg-red-600 text-white px-3 py-1 rounded text-sm transition-colors"
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
