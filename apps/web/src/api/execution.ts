// Placeholder for execution.ts
import client from "./client";
import { ExecutionResult, Language, TestCase } from "../types";

export async function runCode(
  language: Language, code: string, stdIn: string
): Promise<{ jobId: string }> {
  const { data } = await client.post("/api/execution/run", { language, code, stdIn });
  return data;
}

export async function testCode(
  language: Language, code: string, testCases: TestCase[]
): Promise<{ jobId: string }> {
  const { data } = await client.post("/api/execution/test", { language, code, testCases });
  return data;
}

export async function pollResult(jobId: string): Promise<ExecutionResult> {
  const { data } = await client.get(`/api/execution/result/${jobId}`);
  return data;
}