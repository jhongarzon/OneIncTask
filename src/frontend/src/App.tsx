import { useState } from 'react';
import { ProcessingPage } from '@/features/processing/ProcessingPage';
import { HistoryPage } from '@/features/history/HistoryPage';
import { cn } from '@/lib/utils';

type Tab = 'processing' | 'history';

export default function App() {
  const [activeTab, setActiveTab] = useState<Tab>('processing');

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b">
        <div className="max-w-4xl mx-auto px-4 py-4">
          <h1 className="text-xl font-bold text-gray-900">OneIncTask</h1>
          <p className="text-sm text-gray-500">Long-Running Job Processing</p>
        </div>
      </header>

      <nav className="bg-white border-b">
        <div className="max-w-4xl mx-auto px-4 flex gap-1">
          {(['processing', 'history'] as Tab[]).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={cn(
                'px-4 py-3 text-sm font-medium capitalize border-b-2 transition-colors',
                activeTab === tab
                  ? 'border-blue-600 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              )}
            >
              {tab}
            </button>
          ))}
        </div>
      </nav>

      <main className="max-w-4xl mx-auto px-4 py-6">
        <div style={{ display: activeTab === 'processing' ? 'block' : 'none' }}>
          <ProcessingPage />
        </div>
        {activeTab === 'history' && <HistoryPage />}
      </main>
    </div>
  );
}
