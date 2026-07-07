export type ProjectStatus = "Draft" | "Active" | "Archived";

export interface Project {
  id: string;
  ownerId: string;
  name: string;
  description: string;
  status: ProjectStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProjectPayload {
  name: string;
  description: string;
}

export interface UpdateProjectPayload extends CreateProjectPayload {
  status: ProjectStatus;
}

export interface ProjectOwnerReportItem {
  projectId: string;
  projectName: string;
  ownerId: string;
  ownerEmail: string;
  status: ProjectStatus;
}

export interface ProjectOwnerReport {
  projects: ProjectOwnerReportItem[];
}
