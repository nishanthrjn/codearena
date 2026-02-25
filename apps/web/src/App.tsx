// H-2/C-1: AuthCallback removed — API now sets HttpOnly cookie before redirecting to /
// App.tsx no longer needs to read a token from the URL or localStorage.
// PrivateRoute calls GET /api/auth/me to verify the session cookie is valid.

import { useState, useEffect } from "react";
import { BrowserRouter, Route, Routes, Navigate } from "react-router-dom";
import { EditorPage } from "./pages/EditorPage";
import { LoginPage } from "./pages/LoginPage";
import { SnippetsPage } from "./pages/SnippetsPage";
import client from "./api/client";

type AuthState = "loading" | "authenticated" | "unauthenticated";

function useAuthCheck(): AuthState {
  const [state, setState] = useState<AuthState>("loading");

  useEffect(() => {
    client.get("/api/auth/me")
      .then(() => setState("authenticated"))
      .catch(() => setState("unauthenticated"));
  }, []);

  return state;
}

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const auth = useAuthCheck();
  if (auth === "loading") return <div className="flex items-center justify-center h-screen bg-gray-900 text-gray-400">Loading…</div>;
  return auth === "authenticated" ? <>{children}</> : <Navigate to="/login" />;
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        {/* /auth/callback is no longer needed — API redirects straight to / after setting the cookie */}
        <Route path="/auth/callback" element={<Navigate to="/" replace />} />
        <Route path="/" element={<PrivateRoute><SnippetsPage /></PrivateRoute>} />
        <Route path="/editor" element={<PrivateRoute><EditorPage /></PrivateRoute>} />
        <Route path="/editor/:id" element={<PrivateRoute><EditorPage /></PrivateRoute>} />
      </Routes>
    </BrowserRouter>
  );
}