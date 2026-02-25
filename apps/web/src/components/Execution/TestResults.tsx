import { useEditorStore } from "../../store/editorStore";

export function TestResults() {
  const { executionResult } = useEditorStore();
  const results = executionResult?.testResults;
  if (!results) return null;

  const passed = results.filter(r => r.passed).length;

  return (
    <div className="p-3 overflow-y-auto">
      <div className={`text-sm font-bold mb-3 ${passed === results.length ? "text-green-400" : "text-red-400"}`}>
        {passed}/{results.length} Test Cases Passed
      </div>
      {results.map((r, i) => (
        <div key={i} className={`rounded p-3 mb-2 text-xs ${r.passed ? "bg-green-950 border border-green-700" : "bg-red-950 border border-red-700"}`}>
          <div className="flex justify-between font-semibold">
            <span>{r.name}</span>
            <span className={r.passed ? "text-green-400" : "text-red-400"}>{r.passed ? "✓ PASS" : "✗ FAIL"}</span>
            <span className="text-gray-400">{r.durationMs}ms</span>
          </div>
          {!r.passed && (
            <div className="mt-2 space-y-1">
              <div><span className="text-gray-400">Expected:</span> <code className="text-yellow-300">{r.expected}</code></div>
              <div><span className="text-gray-400">Got:</span> <code className="text-white">{r.stdout}</code></div>
              {r.stderr && <div className="text-red-300">Stderr: {r.stderr}</div>}
            </div>
          )}
        </div>
      ))}
    </div>
  );
}