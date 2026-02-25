import client from "./client";
import type { GitHubRepo } from "../types";

export async function listRepos(): Promise<GitHubRepo[]> {
    const { data } = await client.get<GitHubRepo[]>("/api/github/repos");
    return data;
}

export async function pushSnippet(payload: {
    snippetId: string;
    repoFullName: string;
    branch: string;
    commitMessage: string;
}): Promise<{ commitSha: string }> {
    const { data } = await client.post<{ commitSha: string }>("/api/github/push", payload);
    return data;
}
