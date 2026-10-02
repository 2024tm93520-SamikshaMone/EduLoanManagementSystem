const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";

const getToken = () => sessionStorage.getItem("eduloan_access_token");

async function handleResponse<T>(response: Response): Promise<T> {
  const text = await response.text();

  if (!response.ok) {
    throw new Error(text || `Request failed with status ${response.status}`);
  }
  if (!text) {
    throw new Error("Empty response received from server.");
  }

  const result = JSON.parse(text);
  if (!result.success) {
    throw new Error(result.message || "Request failed.");
  }
  return result.data;
}

export interface WorkflowStep {
  id: string;
  actorName: string;
  actorRole: string;
  action: string;
  comments: string | null;
  actionedAt: string;
}

export interface WorkflowResult {
  applicationId: string;
  applicationNumber: string;
  status: string;
}

function authHeaders(): Record<string, string> {
  return {
    "Content-Type": "application/json",
    Authorization: `Bearer ${getToken()}`,
  };
}

export async function getWorkflowHistory(applicationId: string): Promise<WorkflowStep[]> {
  const response = await fetch(
    `${API_BASE_URL}/admin/applications/${applicationId}/workflow-history`,
    { headers: authHeaders() }
  );
  return handleResponse<WorkflowStep[]>(response);
}

async function postAction(
  applicationId: string,
  action: string,
  body?: object
): Promise<WorkflowResult> {
  const response = await fetch(
    `${API_BASE_URL}/admin/applications/${applicationId}/${action}`,
    {
      method: "POST",
      headers: authHeaders(),
      body: JSON.stringify(body ?? {}),
    }
  );
  return handleResponse<WorkflowResult>(response);
}

export const startReview = (applicationId: string) =>
  postAction(applicationId, "start-review");

export const escalateToManualReview = (applicationId: string, comments?: string) =>
  postAction(applicationId, "manual-review", { comments });

export const requestInfo = (applicationId: string, comments: string) =>
  postAction(applicationId, "request-info", { comments });

export const approveApplication = (applicationId: string, comments?: string) =>
  postAction(applicationId, "approve", { comments });

export const rejectApplication = (applicationId: string, comments: string) =>
  postAction(applicationId, "reject", { comments });

export const sendToFinance = (applicationId: string) =>
  postAction(applicationId, "send-to-finance");

export const confirmProcessed = (applicationId: string, confirmationReference: string) =>
  postAction(applicationId, "confirm-processed", { confirmationReference });
