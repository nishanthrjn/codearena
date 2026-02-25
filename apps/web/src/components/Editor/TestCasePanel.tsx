import { TestCase } from "../../types";
import { useEditorStore } from "../../store/editorStore";

export function TestCasePanel() {
  const { testCases, setTestCases } = useEditorStore();

  const add = () => setTestCases([
    ...testCases,
    { name: `Case ${testCases.length + 1}`, stdIn: "", expected: "", orderIndex: testCases.length }
  ]);

  const remove = (i: number) => setTestCases(testCases.filter((_, idx) => idx !== i));

  const update = (i: number, field: keyof TestCase, value: string) => {
    const updated = [...testCases];
    (updated[i] as any)[field] = value;
    setTestCases(updated);
  };

  return (
    <div className="flex flex-col gap-3 overflow-y-auto p-2">
      {testCases.map((tc, i) => (
        <div key={i} className="bg-gray-800 rounded p-3 text-sm">
          <div className="flex justify-between mb-2">
            <input
              className="bg-transparent text-white font-semibold flex-1"
              value={tc.name}
              onChange={e => update(i, "name", e.target.value)}
            />
            <button onClick={() => remove(i)} className="text-red-400 text-xs ml-2">Remove</button>
          </div>
          <label className="text-gray-400 text-xs">Input (stdin)</label>
          <textarea
            className="w-full bg-gray-900 text-white rounded p-2 mt-1 font-mono text-xs"
            rows={2} value={tc.stdIn}
            onChange={e => update(i, "stdIn", e.target.value)}
          />
          <label className="text-gray-400 text-xs">Expected Output</label>
          <textarea
            className="w-full bg-gray-900 text-white rounded p-2 mt-1 font-mono text-xs"
            rows={2} value={tc.expected}
            onChange={e => update(i, "expected", e.target.value)}
          />
        </div>
      ))}
      {testCases.length < 10 && (
        <button
          onClick={add}
          className="text-blue-400 border border-blue-600 rounded py-1 text-sm hover:bg-blue-900"
        >
          + Add Test Case
        </button>
      )}
    </div>
  );
}