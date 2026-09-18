export interface Department {
  id: number;
  name: string;
  isActive: boolean;
}

export interface College {
  id: number;
  name: string;
  city: string;
  isActive: boolean;
}

export interface Course {
  id: number;
  name: string;
  level: string;
  standardDurationMonths: number;
  isActive: boolean;
}

export type MasterDataEntity = "departments" | "colleges" | "courses";
