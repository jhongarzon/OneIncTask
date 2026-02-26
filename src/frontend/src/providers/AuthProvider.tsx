import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { getAuthToken } from '@/lib/api-client';

interface AuthContextType {
  token: string | null;
  isLoading: boolean;
  error: string | null;
}

const AuthContext = createContext<AuthContextType>({ token: null, isLoading: true, error: null });

export function useAuth() {
  return useContext(AuthContext);
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(localStorage.getItem('jwt_token'));
  const [isLoading, setIsLoading] = useState(!token);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (token) return;

    getAuthToken()
      .then((t) => {
        localStorage.setItem('jwt_token', t);
        setToken(t);
      })
      .catch((err) => {
        setError(err.message);
      })
      .finally(() => {
        setIsLoading(false);
      });
  }, [token]);

  return (
    <AuthContext.Provider value={{ token, isLoading, error }}>
      {children}
    </AuthContext.Provider>
  );
}
