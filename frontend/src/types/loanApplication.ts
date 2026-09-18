export interface LookupItem {
  id: number;
  name: string;
}

export interface ApplicationDocument {
  id: string;
  documentType: string;
  fileName: string;
  fileSizeBytes: number;
  uploadedAt: string;
}

export interface LoanApplicationSummary {
  id: string;
  applicationNumber: string;
  collegeName: string;
  courseName: string;
  requestedAmount: number;
  status: "Draft" | "Submitted" | "Cancelled";
  createdAt: string;
  submittedAt: string | null;
}

export interface LoanApplicationDetail {
  id: string;
  applicationNumber: string;
  employeeId: string;
  collegeId: number;
  collegeName: string;
  courseId: number;
  courseName: string;
  specialization: string;
  courseDurationMonths: number;
  totalEducationFees: number;
  requestedAmount: number;
  requestedTenureMonths: number;
  educationPurpose: string;
  status: "Draft" | "Submitted" | "Cancelled";
  createdAt: string;
  submittedAt: string | null;
  documents: ApplicationDocument[];
}

export interface LoanApplicationFormValues {
  collegeId: number;
  courseId: number;
  specialization: string;
  courseDurationMonths: number;
  totalEducationFees: number;
  requestedAmount: number;
  requestedTenureMonths: number;
  educationPurpose: string;
}
