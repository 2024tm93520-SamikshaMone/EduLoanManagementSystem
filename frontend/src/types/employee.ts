export interface EmployeeProfile {
  id: string;
  employeeCode: string;
  fullName: string;
  email: string;
  role: string;
  departmentName: string | null;
  designation: string | null;
  dateOfJoining: string;
  tenureMonths: number;
  monthlySalary: number;
  phoneNumber: string | null;
}
