import { useState } from "react";
import { CodeEditor } from "../components/Editor/CodeEditor";
import { LanguageSelector } from "../components/Editor/LanguageSelector";
import { TestCasePanel } from "../components/Editor/TestCasePanel";
import { OutputPanel } from "../components/Execution/OutputPanel";
import { TestResults } from "../components/Execution/TestResults";
import { useEditorStore } from "../store/editorStore";
import { useExecution } from "../hooks/useExecution";
import client from "../api/client";

export function EditorPage() {
  const { title, tags, testCases, language, code, setTitle, setTags, snippetId } = useEditorStore();
  const { run, test, stdin, setStdin } = useExecution();
  const [activeTab, setActiveTab] = useState<"output" | "tests" | "stdin">("output");
  const [saving, setSaving] = useState(false);
  const [pushModal, setPushModal] = useState(false);

  async function save() {
    setSaving(true);
    try {
      if (snippetId) {
        await client.put(`/api/snippets/${snippetId}`, { title, language, code, tags, testCases });
      } else {
        await client.post("/api/snippets", { title, language, code, tags, testCases });
      }
      alert("Saved!");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="flex flex-col h-screen bg-gray-900 text-white">
      {/* Header bar */}
      <div className="flex items-center gap-3 px-4 py-2 bg-gray-800 border-b border-gray-700">
        <input
          className="bg-transparent text-lg font-semibold flex-1 outline-none"
          value={title}
          onChange={e => setTitle(e.target.value)}
          placeholder="Snippet title…"
        />
        <input
          className="bg-gray-700 text-sm rounded px-2 py-1 w-48"
          value={tags} onChange={e => setTags(e.target.value)}
          placeholder="tags, comma-separated"
        />
        <LanguageSelector />
        <button
          onClick={save} disabled={saving}
          className="bg-blue-600 hover:bg-blue-500 px-3 py-1 rounded text-sm"
        >
          {saving ? "Saving…" : "Save"}
        </button>
        <button
          onClick={() => setPushModal(true)}
          className="bg-gray-600 hover:bg-gray-500 px-3 py-1 rounded text-sm"
        >
          Push to GitHub
        </button>
      </div>

      {/* Main layout */}
      <div className="flex flex-1 overflow-hidden">
        {/* Left: Editor */}
        <div className="flex-1 flex flex-col">
          <div className="flex-1 overflow-hidden">
            <CodeEditor />
          </div>
          {/* Run controls */}
          <div className="flex items-center gap-2 p-2 bg-gray-800 border-t border-gray-700">
            <button
              onClick={run}
              className="bg-green-700 hover:bg-green-600 px-4 py-1.5 rounded text-sm font-medium"
            >▶ Run</button>
            <button
              onClick={test}
              disabled={testCases.length === 0}
              className="bg-yellow-700 hover:bg-yellow-600 px-4 py-1.5 rounded text-sm font-medium disabled:opacity-40"
            >⚡ Test All</button>
          </div>
        </div>

        {/* Right: Panels */}
        <div className="w-96 flex flex-col border-l border-gray-700">
          {/* Tabs */}
          <div className="flex border-b border-gray-700">
            {(["output", "stdin", "tests"] as const).map(tab => (
              <button
                key={tab}
                onClick={() => setActiveTab(tab)}
                className={`flex-1 py-2 text-xs capitalize ${activeTab === tab ? "bg-gray-700 text-white" : "text-gray-400 hover:text-white"}`}
              >{tab}</button>
            ))}
          </div>
          <div className="flex-1 overflow-hidden">
            {activeTab === "output" && <OutputPanel />}
            {activeTab === "tests"  && <TestCasePanel />}
            {activeTab === "stdin"  && (
              <textarea
                className="w-full h-full bg-gray-950 text-green-200 font-mono text-sm p-3 resize-none"
                value={stdin} onChange={e => setStdin(e.target.value)}
                placeholder="Standard input (stdin)…"
              />
            )}
          </div>
          {/* Test results overlay on output tab */}
          {activeTab === "output" && <TestResults />}
        </div>
      </div>

      {pushModal && <GitHubPushModal onClose={() => setPushModal(false)} />}
    </div>
  );
}

function GitHubPushModal({ onClose }: { onClose: () => void }) {
  const { snippetId } = useEditorStore();
  const [repos, setRepos] = useState<{ fullName: string; defaultBranch: string }[]>([]);
  const [repo, setRepo] = useState("");
  const [branch, setBranch] = useState("main");
  const [msg, setMsg] = useState("feat: add snippet from CodeArena");
  const [loading, setLoading] = useState(false);

  useState(() => {
    client.get("/api/github/repos").then(r => setRepos(r.data));
  });

  async function push() {
    if (!snippetId) { alert("Save the snippet first!"); return; }
    setLoading(true);
    try {
      await client.post("/api/github/push", {
        snippetId, repoFullName: repo, branch, commitMessage: msg,
      });
      alert("Pushed to GitHub!");
      onClose();
    } catch (e: any) {
      alert("Push failed: " + (e.response?.data?.error ?? e.message));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
      <div className="bg-gray-800 rounded-lg p-6 w-96 space-y-4">
        <h2 className="text-lg font-bold">Push to GitHub</h2>
        <div>
          <label className="text-sm text-gray-400">Repository</label>
          <select
            className="w-full mt-1 bg-gray-700 rounded px-3 py-2 text-sm"
            value={repo} onChange={e => { setRepo(e.target.value); setBranch(repos.find(r => r.fullName === e.target.value)?.defaultBranch ?? "main"); }}
          >
            <option value="">Select a repo…</option>
            {repos.map(r => <option key={r.fullName} value={r.fullName}>{r.fullName}</option>)}
          </select>
        </div>
        <div>
          <label className="text-sm text-gray-400">Branch</label>
          <input className="w-full mt-1 bg-gray-700 rounded px-3 py-2 text-sm"
            value={branch} onChange={e => setBranch(e.target.value)} />
        </div>
        <div>
          <label className="text-sm text-gray-400">Commit message</label>
          <input className="w-full mt-1 bg-gray-700 rounded px-3 py-2 text-sm"
            value={msg} onChange={e => setMsg(e.target.value)} />
        </div>
        <div className="flex gap-3 justify-end pt-2">
          <button onClick={onClose} className="px-4 py-2 text-sm bg-gray-600 rounded">Cancel</button>
          <button onClick={push} disabled={!repo || loading}
            className="px-4 py-2 text-sm bg-green-700 rounded disabled:opacity-40">
            {loading ? "Pushing…" : "Push"}
          </button>
        </div>
      </div>
    </div>
  );
}