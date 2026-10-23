export interface ApplicationConsistencyFlag {
  code: string;
  title: string;
  severity: "High" | "Medium" | "Low" | string;
  description: string;
}

export interface DuplicateApplicationReference {
  applicationId: string;
  applicationNumber: string;
  status: string;
}

export interface ApplicationConsistencySummary {
  applicationId: string;
  flags: ApplicationConsistencyFlag[];
  potentialDuplicates: DuplicateApplicationReference[];
  disclaimer: string;
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";

export async function getApplicationConsistencyChecks(applicationId: string): Promise<ApplicationConsistencySummary> {
  const token = sessionStorage.getItem("eduloan_access_token");
  const response = await fetch(`${API_BASE_URL}/admin/applications/${applicationId}/consistency-checks`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  const body = await response.json();
  if (!response.ok || !body.success || !body.data) {
    throw new Error(body.message || "Could not load application consistency checks.");
  }
  return body.data as ApplicationConsistencySummary;
}
