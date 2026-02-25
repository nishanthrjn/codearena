import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import client from "../api/client";
import { useEditorStore } from "../store/editorStore";
import type { Snippet } from "../types";

export function SnippetsPage() {
    const [snippets, setSnippets] = useState<Snippet[]>([]);
    const [loading, setLoading] = useState(true);
    const navigate = useNavigate();
    const loadSnippet = useEditorStore(s => s.loadSnippet);

    useEffect(() => {
        client.get<Snippet[]>("/api/snippets")
            .then(r => setSnippets(r.data))
            .finally(() => setLoading(false));
    }, []);

    function open(s: Snippet) {
        loadSnippet(s);
        navigate("/editor");
    }

    async function newSnippet() {
        navigate("/editor");
    }

    async function logout() {
        await client.post("/api/auth/logout").catch(() => { });
        window.location.href = "/login";
    }

    return (
        <div style={{ minHeight: "100vh", background: "#0a0a14", color: "#f1f5f9", fontFamily: "Inter, system-ui, sans-serif" }}>
            {/* Header */}
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "1rem 2rem", borderBottom: "1px solid rgba(255,255,255,0.08)", background: "rgba(255,255,255,0.03)" }}>
                <h1 style={{ margin: 0, fontSize: "1.25rem", fontWeight: 800, background: "linear-gradient(135deg,#7c3aed,#06b6d4)", WebkitBackgroundClip: "text", WebkitTextFillColor: "transparent" }}>
                    ⚡ CodeArena
                </h1>
                <div style={{ display: "flex", gap: "0.75rem" }}>
                    <button onClick={newSnippet} style={{ padding: "0.5rem 1.25rem", background: "linear-gradient(135deg,#7c3aed,#4f46e5)", border: "none", borderRadius: 8, color: "#fff", fontWeight: 600, cursor: "pointer" }}>
                        + New Snippet
                    </button>
                    <button onClick={logout} style={{ padding: "0.5rem 1rem", background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.1)", borderRadius: 8, color: "#94a3b8", cursor: "pointer" }}>
                        Sign out
                    </button>
                </div>
            </div>

            {/* Body */}
            <div style={{ maxWidth: 900, margin: "2rem auto", padding: "0 1.5rem" }}>
                {loading ? (
                    <p style={{ color: "#475569", textAlign: "center" }}>Loading snippets…</p>
                ) : snippets.length === 0 ? (
                    <div style={{ textAlign: "center", paddingTop: "4rem" }}>
                        <p style={{ color: "#475569", fontSize: "1.1rem" }}>No snippets yet.</p>
                        <button onClick={newSnippet} style={{ marginTop: "1rem", padding: "0.75rem 2rem", background: "linear-gradient(135deg,#7c3aed,#4f46e5)", border: "none", borderRadius: 10, color: "#fff", fontWeight: 600, cursor: "pointer", fontSize: "0.95rem" }}>
                            Create your first snippet
                        </button>
                    </div>
                ) : (
                    <div style={{ display: "grid", gap: "1rem" }}>
                        {snippets.map(s => (
                            <div key={s.id} onClick={() => open(s)} style={{ padding: "1.25rem 1.5rem", background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 12, cursor: "pointer", transition: "border-color .2s" }}
                                onMouseEnter={e => (e.currentTarget.style.borderColor = "rgba(124,58,237,0.5)")}
                                onMouseLeave={e => (e.currentTarget.style.borderColor = "rgba(255,255,255,0.08)")}>
                                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
                                    <div>
                                        <span style={{ fontWeight: 700, fontSize: "1rem" }}>{s.title}</span>
                                        <span style={{ marginLeft: "0.75rem", fontSize: "0.75rem", padding: "0.2rem 0.6rem", borderRadius: 6, background: "rgba(124,58,237,0.2)", color: "#a78bfa" }}>{s.language}</span>
                                    </div>
                                    <span style={{ fontSize: "0.75rem", color: "#475569" }}>{new Date(s.updatedAt).toLocaleDateString()}</span>
                                </div>
                                {s.tags && <p style={{ margin: "0.4rem 0 0", fontSize: "0.8rem", color: "#64748b" }}>{s.tags}</p>}
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}
