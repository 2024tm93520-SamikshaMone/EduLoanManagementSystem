const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ??
  "https://localhost:7001/api/v1";

const getToken = () =>
  sessionStorage.getItem("eduloan_access_token");

async function handleResponse<T>(response: Response): Promise<T> {
  const text = await response.text();

  if (!response.ok) {
    throw new Error(
      text || `Request failed with status ${response.status}`
    );
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

export async function getAdminLoanApplications() {
  const response = await fetch(
    `${API_BASE_URL}/admin/applications`,
    {
      headers: {
        Authorization: `Bearer ${getToken()}`,
      },
    }
  );

  return handleResponse<AdminLoanApplication[]>(response);
}

export async function getAdminLoanApplicationById(id: string) {
  const response = await fetch(
    `${API_BASE_URL}/admin/applications/${id}`,
    {
      headers: {
        Authorization: `Bearer ${getToken()}`,
      },
    }
  );

  return handleResponse<AdminLoanApplicationDetail>(response);
}

export interface AdminLoanApplication {
  id: string;
  applicationNumber: string;
  employeeCode: string;
  employeeName: string;
  departmentName: string;
  collegeName: string;
  courseName: string;
  requestedAmount: number;
  submittedAt: string | null;
  status: string;
}

export interface AdminLoanApplicationDetail
  extends AdminLoanApplication {
  employeeId: string;
  email: string;
  designation: string;
  dateOfJoining: string;
  monthlySalary: number;
  phoneNumber: string | null;
  collegeCity: string;
  courseLevel: string;
  specialization: string;
  courseDurationMonths: number;
  totalEducationFees: number;
  requestedTenureMonths: number;
  educationPurpose: string;
  createdAt: string;
}