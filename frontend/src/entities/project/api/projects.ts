import { projectsApi } from "@/shared/api/client";
import type {
  CreateProjectPayload,
  Project,
  ProjectOwnerReport,
  UpdateProjectPayload,
} from "../model/types";

export function getProjects() {
  return projectsApi.get<Project[]>("/projects");
}

export function createProject(payload: CreateProjectPayload) {
  return projectsApi.post<Project>("/projects", payload);
}

export function updateProject(projectId: string, payload: UpdateProjectPayload) {
  return projectsApi.put<Project>(`/projects/${projectId}`, payload);
}

export function deleteProject(projectId: string) {
  return projectsApi.delete<void>(`/projects/${projectId}`);
}

export function getProjectOwnerReport() {
  return projectsApi.get<ProjectOwnerReport>("/projects/report/owners");
}
