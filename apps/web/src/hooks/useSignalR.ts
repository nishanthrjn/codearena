// H-2: No localStorage token — cookie auth is automatic via withCredentials.
// L-2: Single persistent HubConnection for the session lifetime.
//      Use JoinJob/LeaveJob to switch rooms instead of reconnecting on every jobId change.
import { useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import { API_BASE } from "../api/client";
import { useEditorStore } from "../store/editorStore";

// Singleton hub connection — created once, reused across the app
let _hub: signalR.HubConnection | null = null;

function getHub(): signalR.HubConnection {
  if (!_hub) {
    _hub = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/execution`, {
        // H-2: No localStorage token — the browser sends the HttpOnly ca_jwt cookie automatically.
        // withCredentials is set on the axios client; SignalR uses the cookie via the browser too.
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Start once and let withAutomaticReconnect handle reconnects
    _hub.start().catch(err =>
      console.error("[SignalR] Could not connect to execution hub:", err)
    );
  }
  return _hub;
}

export function useSignalR(jobId: string | null) {
  const hubRef = useRef<signalR.HubConnection | null>(null);
  const appendOutput = useEditorStore(s => s.appendOutput);

  useEffect(() => {
    const hub = getHub();
    hubRef.current = hub;

    // L-2: Register output handler once; filter by jobId in handler
    const handler = (incomingJobId: string, chunk: string) => {
      if (incomingJobId === jobId) appendOutput(chunk);
    };
    hub.on("ExecutionOutput", handler);

    // Join the specific job group when jobId is set
    if (jobId) {
      if (hub.state === signalR.HubConnectionState.Connected) {
        hub.invoke("JoinJob", jobId).catch(console.error);
      } else {
        // Wait for the connection to be established, then join
        hub.onreconnected(() => {
          hub.invoke("JoinJob", jobId).catch(console.error);
        });
      }
    }

    return () => {
      // Leave the group and remove the handler — but keep the connection alive
      hub.off("ExecutionOutput", handler);
      if (jobId && hub.state === signalR.HubConnectionState.Connected) {
        hub.invoke("LeaveJob", jobId).catch(() => { /* non-fatal */ });
      }
    };
  }, [jobId, appendOutput]);

  return hubRef;
}