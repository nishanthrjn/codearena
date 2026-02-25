import MonacoEditor from "@monaco-editor/react";
import { useEditorStore } from "../../store/editorStore";

const LANGUAGE_MAP: Record<string, string> = {
  csharp: "csharp", python: "python", javascript: "javascript",
  c: "c", cpp: "cpp",
};

export function CodeEditor() {
  const { code, language, setCode } = useEditorStore();

  return (
    <MonacoEditor
      height="100%"
      language={LANGUAGE_MAP[language] ?? "plaintext"}
      value={code}
      onChange={(v) => setCode(v ?? "")}
      theme="vs-dark"
      options={{
        fontSize: 14,
        minimap: { enabled: false },
        scrollBeyondLastLine: false,
        automaticLayout: true,
        tabSize: 4,
      }}
    />
  );
}