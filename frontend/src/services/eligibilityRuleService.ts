import type { ApiResponse } from "../types/auth";

const API_BASE_URL =
  (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env?.VITE_API_BASE_URL ??
  "https://localhost:7001/api/v1";

export class EligibilityRuleError extends Error {}

export interface EligibilityRule {
  id: number;
  ruleName: string;
  ruleCategory: string;
  conditionField: string;
  operator: string;
  conditionValue: number;
  errorMessage: string;
  severity: "Blocking" | "Warning";
  isActive: boolean;
}

export type EligibilityRuleFormValues = Omit<EligibilityRule, "id">;

export const CONDITION_FIELDS = [
  "TenureMonths",
  "MonthlySalary",
  "RequestedAmount",
  "RequestedTenureMonths",
  "TotalEducationFees",
  "LoanToMonthlySalaryRatio",
] as const;

export const OPERATORS = [">=", "<=", ">", "<", "==", "!="] as const;

function getAuthHeader(): Record<string, string> {
  const token = sessionStorage.getItem("eduloan_access_token");
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function handle<T>(response: Response): Promise<T> {
  const body: ApiResponse<T> = await response.json();
  if (!response.ok || !body.success || body.data === null) {
    throw new EligibilityRuleError(body.message || "Something went wrong. Please try again.");
  }
  return body.data;
}

export async function getEligibilityRules(): Promise<EligibilityRule[]> {
  const res = await fetch(`${API_BASE_URL}/admin/eligibility-rules`, {
    headers: { "Content-Type": "application/json", ...getAuthHeader() },
  });
  return handle<EligibilityRule[]>(res);
}

export async function createEligibilityRule(values: EligibilityRuleFormValues): Promise<EligibilityRule> {
  const res = await fetch(`${API_BASE_URL}/admin/eligibility-rules`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...getAuthHeader() },
    body: JSON.stringify(values),
  });
  return handle<EligibilityRule>(res);
}

export async function updateEligibilityRule(id: number, values: EligibilityRuleFormValues): Promise<EligibilityRule> {
  const res = await fetch(`${API_BASE_URL}/admin/eligibility-rules/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...getAuthHeader() },
    body: JSON.stringify(values),
  });
  return handle<EligibilityRule>(res);
}

export async function deleteEligibilityRule(id: number): Promise<void> {
  const res = await fetch(`${API_BASE_URL}/admin/eligibility-rules/${id}`, {
    method: "DELETE",
    headers: getAuthHeader(),
  });
  await handle<object>(res);
}
