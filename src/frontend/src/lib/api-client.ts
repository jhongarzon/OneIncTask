const API_BASE = '/api';

function getAuthHeaders(): HeadersInit {
  const token = localStorage.getItem('jwt_token');
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  return headers;
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: response.statusText }));
    throw new Error(error.message || error.error?.message || response.statusText);
  }
  return response.json();
}

export interface StartJobResponse {
  jobId: string;
}

export interface JobStatusResponse {
  jobId: string;
  status: string;
  currentResult: string;
  processedCharacters: number;
  totalCharacters: number;
  createdAt: string;
  completedAt: string | null;
  errorMessage: string | null;
}

export interface JobHistoryItem {
  jobId: string;
  inputText: string;
  status: number | string;
  processedCharacters: number;
  totalCharacters: number;
  createdAt: string;
  completedAt: string | null;
}

export async function getAuthToken(): Promise<string> {
  const response = await fetch(`${API_BASE}/auth/token`, {
    headers: getAuthHeaders(),
  });
  const data = await handleResponse<{ token: string }>(response);
  return data.token;
}

export async function startJob(inputText: string): Promise<StartJobResponse> {
  const response = await fetch(`${API_BASE}/jobs`, {
    method: 'POST',
    headers: getAuthHeaders(),
    body: JSON.stringify({ inputText }),
  });
  return handleResponse<StartJobResponse>(response);
}

export async function cancelJob(jobId: string): Promise<void> {
  const response = await fetch(`${API_BASE}/jobs/${jobId}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: response.statusText }));
    throw new Error(error.message || error.error?.message || response.statusText);
  }
}

export async function getJobStatus(jobId: string): Promise<JobStatusResponse> {
  const response = await fetch(`${API_BASE}/jobs/${jobId}/status`, {
    headers: getAuthHeaders(),
  });
  return handleResponse<JobStatusResponse>(response);
}

export async function getJobHistory(): Promise<JobHistoryItem[]> {
  const response = await fetch(`${API_BASE}/jobs/history`, {
    headers: getAuthHeaders(),
  });
  return handleResponse<JobHistoryItem[]>(response);
}
