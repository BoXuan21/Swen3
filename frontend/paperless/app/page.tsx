'use client';

import { useRouter } from 'next/navigation';

export default function Home() {
  const router = useRouter();

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center">
      <div className="text-center">
        <div className="mb-8">
          <h1 className="text-6xl font-bold text-gray-800 mb-4">
            📄 Paperless
          </h1>
          <h2 className="text-2xl text-gray-600 mb-2">
            Document Management System
          </h2>
          <p className="text-lg text-gray-500">
            Welcome to your digital document hub
          </p>
        </div>

        <div className="space-y-4">
          <button
            onClick={() => router.push('/dashboard')}
            className="bg-blue-500 hover:bg-blue-600 text-white px-8 py-3 rounded-lg text-lg font-semibold transition-colors shadow-lg hover:shadow-xl"
          >
            🚀 Go to Dashboard
          </button>
        </div>
      </div>
    </div>
  );
}
