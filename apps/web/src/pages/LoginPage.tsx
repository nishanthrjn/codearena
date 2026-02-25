// LoginPage.tsx — liquid glass login page with cookie consent toast before OAuth
import { useState } from "react";
import { API_BASE } from "../api/client";
import { CookieConsentToast } from "../components/CookieConsentToast";

export function LoginPage() {
    const [showConsent, setShowConsent] = useState(false);

    function initiateLogin() {
        // Show cookie consent toast before redirecting to GitHub OAuth
        setShowConsent(true);
    }

    function handleAccept() {
        // User accepted — proceed to GitHub OAuth
        window.location.href = `${API_BASE}/api/auth/login`;
    }

    function handleDecline() {
        setShowConsent(false);
    }

    return (
        <div className="login-page">
            {/* Animated background */}
            <div className="login-bg" aria-hidden="true">
                <div className="login-bg__orb login-bg__orb--1" />
                <div className="login-bg__orb login-bg__orb--2" />
                <div className="login-bg__orb login-bg__orb--3" />
            </div>

            {/* Glass card */}
            <div className="login-card">
                <div className="login-card__glow" aria-hidden="true" />

                {/* Logo / brand */}
                <div className="login-logo" aria-hidden="true">
                    <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="url(#grad)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
                        <defs>
                            <linearGradient id="grad" x1="0%" y1="0%" x2="100%" y2="100%">
                                <stop offset="0%" stopColor="#7c3aed" />
                                <stop offset="100%" stopColor="#06b6d4" />
                            </linearGradient>
                        </defs>
                        <polyline points="16 18 22 12 16 6" />
                        <polyline points="8 6 2 12 8 18" />
                    </svg>
                </div>

                <h1 className="login-title">Welcome to CodeArena</h1>
                <p className="login-subtitle">
                    A blazing-fast code execution sandbox. Sign in with your GitHub account to save
                    snippets, run code in isolated containers, and push directly to your repos.
                </p>

                <div className="login-features">
                    <div className="login-feature">
                        <span className="login-feature__icon">⚡</span>
                        <span>Run code in 5 languages</span>
                    </div>
                    <div className="login-feature">
                        <span className="login-feature__icon">🔒</span>
                        <span>Isolated Docker sandboxes</span>
                    </div>
                    <div className="login-feature">
                        <span className="login-feature__icon">🐙</span>
                        <span>Push snippets to GitHub</span>
                    </div>
                </div>

                <button
                    id="github-login-btn"
                    className="login-btn"
                    onClick={initiateLogin}
                >
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                        <path d="M12 2C6.477 2 2 6.477 2 12c0 4.42 2.865 8.17 6.839 9.49.5.092.682-.217.682-.482 0-.237-.008-.866-.013-1.7-2.782.603-3.369-1.342-3.369-1.342-.454-1.155-1.11-1.463-1.11-1.463-.908-.62.069-.608.069-.608 1.003.07 1.531 1.03 1.531 1.03.892 1.529 2.341 1.087 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.11-4.555-4.943 0-1.091.39-1.984 1.029-2.683-.103-.253-.446-1.27.098-2.647 0 0 .84-.268 2.75 1.026A9.578 9.578 0 0 1 12 6.836a9.59 9.59 0 0 1 2.504.337c1.909-1.294 2.747-1.026 2.747-1.026.546 1.377.202 2.394.1 2.647.64.699 1.028 1.592 1.028 2.683 0 3.842-2.339 4.687-4.566 4.935.359.309.678.919.678 1.852 0 1.336-.012 2.415-.012 2.741 0 .267.18.578.688.48C19.138 20.167 22 16.418 22 12c0-5.523-4.477-10-10-10Z" />
                    </svg>
                    Continue with GitHub
                </button>

                <p className="login-footer">
                    By signing in you agree to our{" "}
                    <a href="/terms" className="login-link">Terms</a> and{" "}
                    <a href="/privacy" className="login-link">Privacy Policy</a>.
                </p>
            </div>

            {/* Liquid glass cookie consent — shown before OAuth redirect */}
            {showConsent && (
                <CookieConsentToast
                    onAccept={handleAccept}
                    onDecline={handleDecline}
                />
            )}

            <style>{`
        .login-page {
          min-height: 100vh;
          display: flex;
          align-items: center;
          justify-content: center;
          background: #0a0a14;
          padding: 1rem;
          font-family: 'Inter', system-ui, sans-serif;
          position: relative;
          overflow: hidden;
        }

        /* ── Animated background orbs ─────────────────── */
        .login-bg {
          position: absolute;
          inset: 0;
          pointer-events: none;
        }
        .login-bg__orb {
          position: absolute;
          border-radius: 50%;
          filter: blur(80px);
          opacity: 0.25;
          animation: orbFloat 12s ease-in-out infinite alternate;
        }
        .login-bg__orb--1 {
          width: 400px; height: 400px;
          background: radial-gradient(circle, #7c3aed, transparent);
          top: -100px; left: -100px;
          animation-delay: 0s;
        }
        .login-bg__orb--2 {
          width: 350px; height: 350px;
          background: radial-gradient(circle, #06b6d4, transparent);
          bottom: -80px; right: -80px;
          animation-delay: -6s;
        }
        .login-bg__orb--3 {
          width: 250px; height: 250px;
          background: radial-gradient(circle, #3b82f6, transparent);
          top: 50%; left: 60%;
          animation-delay: -3s;
        }
        @keyframes orbFloat {
          0%   { transform: translate(0, 0) scale(1); }
          100% { transform: translate(20px, -20px) scale(1.1); }
        }

        /* ── Glass card ───────────────────────────────── */
        .login-card {
          position: relative;
          z-index: 1;
          width: 100%;
          max-width: 420px;
          padding: 2.5rem 2rem;
          background: rgba(255,255,255,0.05);
          backdrop-filter: blur(32px) saturate(180%);
          -webkit-backdrop-filter: blur(32px) saturate(180%);
          border: 1px solid rgba(255,255,255,0.1);
          border-radius: 24px;
          box-shadow:
            0 20px 60px rgba(0,0,0,0.5),
            0 0 0 1px rgba(255,255,255,0.04) inset,
            0 1px 0 rgba(255,255,255,0.12) inset;
          text-align: center;
        }
        .login-card__glow {
          position: absolute;
          inset: -1px;
          border-radius: 25px;
          background: linear-gradient(135deg,
            rgba(124,58,237,0.15) 0%,
            rgba(6,182,212,0.1) 50%,
            rgba(59,130,246,0.1) 100%);
          pointer-events: none;
          z-index: -1;
        }

        /* ── Logo ─────────────────────────────────────── */
        .login-logo {
          display: inline-flex;
          align-items: center;
          justify-content: center;
          width: 64px; height: 64px;
          border-radius: 18px;
          background: rgba(124,58,237,0.15);
          border: 1px solid rgba(124,58,237,0.3);
          margin-bottom: 1.25rem;
        }

        /* ── Text ─────────────────────────────────────── */
        .login-title {
          font-size: 1.5rem;
          font-weight: 800;
          color: #f1f5f9;
          margin: 0 0 0.6rem;
          letter-spacing: -0.02em;
        }
        .login-subtitle {
          font-size: 0.875rem;
          line-height: 1.65;
          color: #64748b;
          margin: 0 0 1.5rem;
        }

        /* ── Features ─────────────────────────────────── */
        .login-features {
          display: flex;
          flex-direction: column;
          gap: 0.5rem;
          margin-bottom: 1.75rem;
          text-align: left;
        }
        .login-feature {
          display: flex;
          align-items: center;
          gap: 0.6rem;
          font-size: 0.82rem;
          color: #475569;
          background: rgba(255,255,255,0.03);
          border: 1px solid rgba(255,255,255,0.06);
          border-radius: 8px;
          padding: 0.5rem 0.75rem;
        }
        .login-feature__icon { font-size: 0.95rem; }

        /* ── Login button ─────────────────────────────── */
        .login-btn {
          display: flex;
          align-items: center;
          justify-content: center;
          gap: 0.6rem;
          width: 100%;
          padding: 0.85rem 1.5rem;
          background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
          border: 1px solid rgba(255,255,255,0.12);
          border-radius: 12px;
          color: #e2e8f0;
          font-size: 0.92rem;
          font-weight: 600;
          cursor: pointer;
          transition: all 0.2s ease;
          box-shadow: 0 4px 16px rgba(0,0,0,0.3);
          letter-spacing: 0.01em;
        }
        .login-btn:hover {
          background: linear-gradient(135deg, #252542 0%, #1e2a4a 100%);
          border-color: rgba(255,255,255,0.2);
          transform: translateY(-2px);
          box-shadow: 0 8px 24px rgba(0,0,0,0.4);
          color: #fff;
        }
        .login-btn:active { transform: translateY(0) scale(0.98); }

        /* ── Footer ───────────────────────────────────── */
        .login-footer {
          margin-top: 1.25rem;
          font-size: 0.75rem;
          color: #334155;
        }
        .login-link {
          color: #7c3aed;
          text-decoration: none;
        }
        .login-link:hover { text-decoration: underline; }
      `}</style>
        </div>
    );
}
