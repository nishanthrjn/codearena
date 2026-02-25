import { useEditorStore } from "../../store/editorStore";

export function OutputPanel() {
  const { executionResult, liveOutput, isRunning } = useEditorStore();

  const content = executionResult
    ? (executionResult.stdout || executionResult.stderr || "")
    : liveOutput;

  return (
    <div className="bg-gray-950 font-mono text-sm text-green-300 p-3 overflow-y-auto h-full">
      {isRunning && !content && <span className="text-gray-400 animate-pulse">Running…</span>}
      {executionResult && (
        <div className={`text-xs mb-2 ${executionResult.status === "done" ? "text-green-400" : "text-red-400"}`}>
          [{executionResult.status.toUpperCase()}] exit={executionResult.exitCode} {executionResult.durationMs}ms
        </div>
      )}
      <pre className="whitespace-pre-wrap break-words">{content}</pre>
    </div>
  );
}