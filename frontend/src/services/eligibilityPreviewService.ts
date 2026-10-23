import type { ApiResponse } from "../types/auth";

const API_BASE_URL = (import.meta as ImportMeta & { env?: { VITE_API_BASE_URL?: string } }).env?.VITE_API_BASE_URL ?? "https://localhost:7001/api/v1";
export interface EligibilityRuleGuideline { id: number; ruleName: string; ruleCategory: string; conditionField: string; operator: string; conditionValue: number; errorMessage: string; severity: "Blocking" | "Warning"; isActive: boolean; }
export interface EligibilityPreview { monthlySalary: number; tenureMonths: number; maximumEligibleAmount: number | null; hasBlockingFailures: boolean; rules: EligibilityRuleGuideline[]; results: { ruleId: number; ruleName: string; ruleCategory: string; severity: string; passed: boolean; evaluatedValue: number; failureMessage: string | null }[]; }
export class EligibilityPreviewError extends Error {}
function auth(): Record<string, string> { const token = sessionStorage.getItem("eduloan_access_token"); return token ? { Authorization: `Bearer ${token}` } : {}; }
async function parse<T>(res: Response): Promise<T> { const body: ApiResponse<T> = await res.json(); if (!res.ok || !body.success || body.data == null) throw new EligibilityPreviewError(body.message || body.errors?.join(" ") || "Could not load eligibility information."); return body.data; }
export async function getEligibilityGuidelines(): Promise<EligibilityRuleGuideline[]> { return parse(await fetch(`${API_BASE_URL}/eligibility/rules`, { headers: { ...auth() } })); }
export async function previewEligibility(input: { requestedAmount: number; requestedTenureMonths: number; totalEducationFees: number }): Promise<EligibilityPreview> { return parse(await fetch(`${API_BASE_URL}/eligibility/preview`, { method: "POST", headers: { "Content-Type": "application/json", ...auth() }, body: JSON.stringify(input) })); }

export interface DocumentRequirement {
  id: number; documentType: string; displayName: string; category: string; isRequired: boolean;
  minimumLoanAmount: number | null; maximumLoanAmount: number | null; employmentType: string | null;
  educationLevel: string | null; requiresCollateral: boolean; isActive: boolean; displayOrder: number;
}
export async function getDocumentRequirements(): Promise<DocumentRequirement[]> {
  return parse(await fetch(`${API_BASE_URL}/module6/document-requirements`, { headers: { ...auth() } }));
}
