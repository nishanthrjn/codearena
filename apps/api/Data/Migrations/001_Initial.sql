-- Placeholder for 001_Initial.sql
-- Run by EF Core Migrate or manually
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE "Users" (
    "Id"             UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "GitHubId"       BIGINT NOT NULL UNIQUE,
    "Login"          VARCHAR(255) NOT NULL,
    "Email"          VARCHAR(255) NOT NULL DEFAULT '',
    "AvatarUrl"      TEXT NOT NULL DEFAULT '',
    "EncryptedToken" TEXT NOT NULL DEFAULT '',
    "CreatedAt"      TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
);

CREATE TABLE "Snippets" (
    "Id"         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserId"     UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Title"      VARCHAR(200) NOT NULL,
    "Language"   VARCHAR(50)  NOT NULL,
    "Code"       TEXT         NOT NULL DEFAULT '',
    "Tags"       VARCHAR(500) NOT NULL DEFAULT '',
    "Slug"       VARCHAR(200) NOT NULL,
    "CreatedAt"  TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    "UpdatedAt"  TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    UNIQUE ("UserId", "Slug")
);

CREATE TABLE "TestCases" (
    "Id"         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "SnippetId"  UUID NOT NULL REFERENCES "Snippets"("Id") ON DELETE CASCADE,
    "Name"       VARCHAR(200) NOT NULL DEFAULT '',
    "StdIn"      TEXT         NOT NULL DEFAULT '',
    "Expected"   TEXT         NOT NULL DEFAULT '',
    "OrderIndex" INT          NOT NULL DEFAULT 0
);

CREATE TABLE "ExecutionJobs" (
    "Id"            UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserId"        UUID NOT NULL,
    "Language"      VARCHAR(50)  NOT NULL,
    "Code"          TEXT         NOT NULL DEFAULT '',
    "StdIn"         TEXT         NOT NULL DEFAULT '',
    "JobType"       VARCHAR(20)  NOT NULL DEFAULT 'run',
    "Status"        VARCHAR(20)  NOT NULL DEFAULT 'queued',
    "Output"        TEXT,
    "ExitCode"      INT,
    "DurationMs"    BIGINT NOT NULL DEFAULT 0,
    "TestCasesJson" JSONB NOT NULL DEFAULT '[]',
    "CreatedAt"     TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
);
CREATE INDEX idx_jobs_status ON "ExecutionJobs"("Status");