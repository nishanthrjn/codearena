import client from "./client";
import type { Snippet, TestCase } from "../types";

export async function listSnippets(): Promise<Snippet[]> {
    const { data } = await client.get<Snippet[]>("/api/snippets");
    return data;
}

export async function getSnippet(id: string): Promise<Snippet> {
    const { data } = await client.get<Snippet>(`/api/snippets/${id}`);
    return data;
}

export async function createSnippet(payload: {
    title: string;
    language: string;
    code: string;
    tags: string;
    testCases: TestCase[];
}): Promise<Snippet> {
    const { data } = await client.post<Snippet>("/api/snippets", payload);
    return data;
}

export async function updateSnippet(
    id: string,
    payload: {
        title: string;
        language: string;
        code: string;
        tags: string;
        testCases: TestCase[];
    }
): Promise<Snippet> {
    const { data } = await client.put<Snippet>(`/api/snippets/${id}`, payload);
    return data;
}

export async function deleteSnippet(id: string): Promise<void> {
    await client.delete(`/api/snippets/${id}`);
}
