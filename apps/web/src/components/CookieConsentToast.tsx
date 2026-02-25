/**
 * CookieConsentToast — liquid-glass style cookie permission toast.
 * Shows before the user initiates GitHub OAuth login.
 * On "Accept" it fires the onAccept callback (which redirects to /api/auth/login).
 * On "Decline" it closes without proceeding.
 */
import { useEffect, useState } from "react";
import "./CookieConsent.css";

interface CookieConsentToastProps {
    onAccept: () => void;
    onDecline: () => void;
}

export function CookieConsentToast({ onAccept, onDecline }: CookieConsentToastProps) {
    const [visible, setVisible] = useState(false);

    // Animate in on mount
    useEffect(() => {
        const t = setTimeout(() => setVisible(true), 50);
        return () => clearTimeout(t);
    }, []);

    function handleAccept() {
        setVisible(false);
        setTimeout(onAccept, 300);
    }

    function handleDecline() {
        setVisible(false);
        setTimeout(onDecline, 300);
    }

    return (
        <div className={`cookie-toast-backdrop ${visible ? "cookie-toast-backdrop--visible" : ""}`}>
            <div className={`cookie-toast ${visible ? "cookie-toast--visible" : ""}`} role="dialog" aria-modal="true" aria-labelledby="cookie-toast-title">

                {/* Decorative blobs */}
                <div className="cookie-blob cookie-blob--1" aria-hidden="true" />
                <div className="cookie-blob cookie-blob--2" aria-hidden="true" />

                <div className="cookie-toast__icon" aria-hidden="true">
                    <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                        <path d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2Z" />
                        <circle cx="8.5" cy="10.5" r="1.5" fill="currentColor" stroke="none" />
                        <circle cx="15.5" cy="10.5" r="1.5" fill="currentColor" stroke="none" />
                        <path d="M9 15a3 3 0 0 0 6 0" />
                        <path d="M12 2a3 3 0 0 1 3 3 3 3 0 0 0 3 3 3 3 0 0 1 3 3" />
                    </svg>
                </div>

                <div className="cookie-toast__body">
                    <h2 id="cookie-toast-title" className="cookie-toast__title">Cookie Permission</h2>
                    <p className="cookie-toast__desc">
                        CodeArena uses a secure <strong>HttpOnly cookie</strong> to keep you signed in.
                        No tracking — just your session, stored safely in an encrypted browser cookie
                        that JavaScript cannot read.
                    </p>
                    <ul className="cookie-toast__list">
                        <li>
                            <span className="cookie-list-icon">🔒</span>
                            <span>HttpOnly &amp; Secure — invisible to scripts</span>
                        </li>
                        <li>
                            <span className="cookie-list-icon">⏱</span>
                            <span>Expires automatically after 8 hours</span>
                        </li>
                        <li>
                            <span className="cookie-list-icon">🚫</span>
                            <span>No third-party tracking or analytics cookies</span>
                        </li>
                    </ul>
                </div>

                <div className="cookie-toast__actions">
                    <button
                        className="cookie-btn cookie-btn--decline"
                        onClick={handleDecline}
                    >
                        Decline
                    </button>
                    <button
                        className="cookie-btn cookie-btn--accept"
                        onClick={handleAccept}
                    >
                        Accept &amp; Continue
                    </button>
                </div>
            </div>
        </div>
    );
}
