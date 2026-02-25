// Placeholder for editorStore.ts
import { create } from "zustand";
import { Language, Snippet, TestCase, ExecutionResult } from "../types";

interface EditorState {
  snippetId:       string | null;
  title:           string;
  language:        Language;
  code:            string;
  tags:            string;
  testCases:       TestCase[];
  executionResult: ExecutionResult | null;
  isRunning:       boolean;
  liveOutput:      string;

  setTitle:    (t: string) => void;
  setLanguage: (l: Language) => void;
  setCode:     (c: string) => void;
  setTags:     (t: string) => void;
  setTestCases:(tc: TestCase[]) => void;
  loadSnippet: (s: Snippet) => void;
  setResult:   (r: ExecutionResult | null) => void;
  setRunning:  (b: boolean) => void;
  appendOutput:(chunk: string) => void;
  resetOutput: () => void;
}

const DEFAULT_CODE: Record<Language, string> = {
  python: `# Python solution\ndef solve():\n    n = int(input())\n    print(n * 2)\n\nsolve()\n`,
  csharp: `using System;\n\nclass Solution {\n    static void Main() {\n        var n = int.Parse(Console.ReadLine()!);\n        Console.WriteLine(n * 2);\n    }\n}\n`,
  javascript: `const lines = require('fs').readFileSync('/dev/stdin','utf8').trim().split('\\n');\nconst n = parseInt(lines[0]);\nconsole.log(n * 2);\n`,
  c: `#include <stdio.h>\nint main() {\n    int n; scanf("%d", &n);\n    printf("%d\\n", n * 2);\n    return 0;\n}\n`,
  cpp: `#include <iostream>\nusing namespace std;\nint main() {\n    int n; cin >> n;\n    cout << n * 2 << endl;\n    return 0;\n}\n`,
};

export const useEditorStore = create<EditorState>((set) => ({
  snippetId:       null,
  title:           "Untitled",
  language:        "python",
  code:            DEFAULT_CODE.python,
  tags:            "",
  testCases:       [],
  executionResult: null,
  isRunning:       false,
  liveOutput:      "",

  setTitle:    (title)     => set({ title }),
  setLanguage: (language)  => set({ language, code: DEFAULT_CODE[language] }),
  setCode:     (code)      => set({ code }),
  setTags:     (tags)      => set({ tags }),
  setTestCases:(testCases) => set({ testCases }),
  loadSnippet: (s) => set({
    snippetId: s.id, title: s.title, language: s.language,
    code: s.code, tags: s.tags, testCases: s.testCases,
  }),
  setResult:   (executionResult) => set({ executionResult }),
  setRunning:  (isRunning)       => set({ isRunning }),
  appendOutput:(chunk) => set(s => ({ liveOutput: s.liveOutput + chunk })),
  resetOutput: () => set({ liveOutput: "", executionResult: null }),
}));