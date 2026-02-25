// Placeholder for useExecution.ts
import { useState } from "react";
import { useEditorStore } from "../store/editorStore";
import { runCode, testCode, pollResult } from "../api/execution";

export function useExecution() {
  const { language, code, testCases, setResult, setRunning, resetOutput } = useEditorStore();
  const [stdin, setStdin] = useState("");

  async function run() {
    resetOutput();
    setRunning(true);
    try {
      const { jobId } = await runCode(language, code, stdin);
      await waitForResult(jobId);
    } catch (e) {
      console.error(e);
    } finally {
      setRunning(false);
    }
  }

  async function test() {
    resetOutput();
    setRunning(true);
    try {
      const { jobId } = await testCode(language, code, testCases);
      await waitForResult(jobId);
    } catch (e) {
      console.error(e);
    } finally {
      setRunning(false);
    }
  }

  async function waitForResult(jobId: string) {
    for (let i = 0; i < 60; i++) {
      await new Promise((r) => setTimeout(r, 500));
      const result = await pollResult(jobId);
      if (result.status !== "pending") {
        setResult(result);
        return;
      }
    }
    setResult({ jobId, status: "error", durationMs: 0, stderr: "Timeout waiting for result" });
  }

  return { run, test, stdin, setStdin };
}