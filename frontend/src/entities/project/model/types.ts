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
