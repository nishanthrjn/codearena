import { Language } from "../../types";
import { useEditorStore } from "../../store/editorStore";

const LANGUAGES: { value: Language; label: string }[] = [
  { value: "python",     label: "Python 3" },
  { value: "csharp",    label: "C# (.NET 8)" },
  { value: "javascript",label: "JavaScript (Node.js)" },
  { value: "c",         label: "C (GCC)" },
  { value: "cpp",       label: "C++ 17 (GCC)" },
];

export function LanguageSelector() {
  const { language, setLanguage } = useEditorStore();
  return (
    <select
      value={language}
      onChange={e => setLanguage(e.target.value as Language)}
      className="bg-gray-800 text-white border border-gray-600 rounded px-2 py-1 text-sm"
    >
      {LANGUAGES.map(l => (
        <option key={l.value} value={l.value}>{l.label}</option>
      ))}
    </select>
  );
}