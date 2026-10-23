export interface SimilarApplicationCase {
  applicationId: string;
  applicationNumber: string;
  outcome: "Approved" | "Rejected";
  collegeName: string;
  courseName: string;
  totalEducationFees: number;
  requestedAmount: number;
  courseDurationMonths: number;
  similarityPercent: number;
}

export interface SimilarApplicationsSummary {
  hasSufficientHistory: boolean;
  historicalCaseCount: number;
  k: number;
  neighbors: SimilarApplicationCase[];
  approvedWeightedPercent: number | null;
  rejectedWeightedPercent: number | null;
  summary: string;
  eligibilityEvaluations: Array<{
    ruleName: string;
    ruleCategory: string;
    severity: string;
    passed: boolean;
    evaluatedValue: string;
    failureMessage: string | null;
    evaluatedAt: string;
  }>;
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";

export async function getSimilarApplications(applicationId: string): Promise<SimilarApplicationsSummary> {
  const token = sessionStorage.getItem("eduloan_access_token");
  const response = await fetch(`${API_BASE_URL}/admin/applications/${applicationId}/similar-cases`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  const body = await response.json();
  if (!response.ok || !body.success || !body.data) {
    throw new Error(body.message || "Could not load similar historical applications.");
  }
  return body.data as SimilarApplicationsSummary;
}
