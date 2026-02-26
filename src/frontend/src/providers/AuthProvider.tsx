import { createContext, useContext, useEffect, useState, useCallback, type ReactNode } from 'react';
import { getAuthToken, setOnUnauthorized } from '@/lib/api-client';

interface AuthContextType {
  token: string | null;
  isLoading: boolean;
  error: string | null;
  refreshToken: () => void;
}

const AuthContext = createContext<AuthContextType>({ token: null, isLoading: true, error: null, refreshToken: () => {} });

export function useAuth() {
  return useContext(AuthContext);
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(localStorage.getItem('jwt_token'));
  const [isLoading, setIsLoading] = useState(!token);
  const [error, setError] = useState<string | null>(null);

  const fetchToken = useCallback(() => {
    setIsLoading(true);
    setError(null);
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
  }, []);

  const refreshToken = useCallback(() => {
    localStorage.removeItem('jwt_token');
    setToken(null);
  }, []);

  useEffect(() => {
    setOnUnauthorized(refreshToken);
  }, [refreshToken]);

  useEffect(() => {
    if (!token) {
      fetchToken();
    }
  }, [token, fetchToken]);

  return (
    <AuthContext.Provider value={{ token, isLoading, error, refreshToken }}>
      {children}
    </AuthContext.Provider>
  );
}
