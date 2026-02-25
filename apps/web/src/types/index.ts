// Placeholder for index.ts
export type Language = "csharp" | "python" | "javascript" | "c" | "cpp";

export interface TestCase {
  id?:        string;
  name:       string;
  stdIn:      string;
  expected:   string;
  orderIndex: number;
}

export interface Snippet {
  id:        string;
  title:     string;
  language:  Language;
  code:      string;
  tags:      string;
  slug:      string;
  updatedAt: string;
  testCases: TestCase[];
}

export interface TestCaseResult {
  name:       string;
  passed:     boolean;
  stdout:     string;
  stderr:     string;
  expected:   string;
  durationMs: number;
}

export interface ExecutionResult {
  jobId:       string;
  status:      "done" | "error" | "pending";
  stdout?:     string;
  stderr?:     string;
  exitCode?:   number;
  durationMs:  number;
  testResults?: TestCaseResult[];
}

export interface GitHubRepo {
  fullName:      string;
  defaultBranch: string;
  private:       boolean;
}