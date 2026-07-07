"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  createProject,
  deleteProject,
  getProjects,
  updateProject,
  type Project,
  type ProjectStatus,
} from "@/entities/project";
import { ApiError } from "@/shared/api/client";
import { RoleGate } from "@/shared/session/ui";
import { ForbiddenScreen } from "@/shared/ui/ForbiddenScreen";

const statuses: ProjectStatus[] = ["Draft", "Active", "Archived"];

type FormState = {
  name: string;
  description: string;
  status: ProjectStatus;
};

const initialForm: FormState = {
  name: "",
  description: "",
  status: "Draft",
};

export default function ProjectsPage() {
  const router = useRouter();
  const [projects, setProjects] = useState<Project[]>([]);
  const [form, setForm] = useState<FormState>(initialForm);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [forbidden, setForbidden] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const editingProject = useMemo(
    () => projects.find((project) => project.id === editingId) ?? null,
    [editingId, projects],
  );

  async function loadProjects() {
    setLoading(true);
    setError(null);
    setForbidden(false);

    try {
      const data = await getProjects();
      setProjects(data);
    } catch (err) {
      handleRequestError(err, "Не удалось загрузить проекты");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadProjects();
  }, []);

  function handleRequestError(err: unknown, fallback: string) {
    if (err instanceof ApiError && err.status === 401) {
      router.replace("/login");
      return;
    }

    if (err instanceof ApiError && err.status === 403) {
      setForbidden(true);
      return;
    }

    setError(err instanceof ApiError ? err.message : fallback);
  }

  function startEdit(project: Project) {
    setEditingId(project.id);
    setForm({
      name: project.name,
      description: project.description,
      status: project.status,
    });
    setNotice(null);
    setError(null);
  }

  function resetForm() {
    setEditingId(null);
    setForm(initialForm);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setNotice(null);

    try {
      if (editingId) {
        await updateProject(editingId, form);
        setNotice("Проект обновлён");
      } else {
        await createProject({
          name: form.name,
          description: form.description,
        });
        setNotice("Проект создан");
      }

      resetForm();
      await loadProjects();
    } catch (err) {
      handleRequestError(err, editingId ? "Не удалось обновить проект" : "Не удалось создать проект");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(projectId: string) {
    setSaving(true);
    setError(null);
    setNotice(null);

    try {
      await deleteProject(projectId);
      setNotice("Проект удалён");
      await loadProjects();
      if (editingId === projectId) {
        resetForm();
      }
    } catch (err) {
      handleRequestError(err, "Не удалось удалить проект");
    } finally {
      setSaving(false);
    }
  }

  return (
    <RoleGate allowedRoles={["User", "Employee", "Moderator", "Admin"]}>
      {forbidden ? (
        <ForbiddenScreen />
      ) : (
        <main className="min-h-[calc(100vh-4rem)] px-4 py-10">
          <section className="mx-auto grid w-full max-w-6xl gap-6 lg:grid-cols-[360px_1fr]">
            <form
              onSubmit={handleSubmit}
              className="h-fit rounded-lg border border-slate-200 bg-white p-5 shadow-sm"
            >
              <div className="mb-5">
                <p className="text-sm font-semibold uppercase tracking-[0.18em] text-slate-500">
                  ProjectsService
                </p>
                <h1 className="mt-2 text-2xl font-semibold text-slate-950">
                  {editingProject ? "Редактирование" : "Новый проект"}
                </h1>
              </div>

              <label className="mb-4 block">
                <span className="mb-1 block text-sm font-medium text-slate-700">Название</span>
                <input
                  value={form.name}
                  onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
                  className="h-10 w-full rounded-md border border-slate-300 px-3 text-sm text-slate-950 outline-none transition focus:border-slate-950"
                  required
                />
              </label>

              <label className="mb-4 block">
                <span className="mb-1 block text-sm font-medium text-slate-700">Описание</span>
                <textarea
                  value={form.description}
                  onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
                  className="min-h-28 w-full resize-y rounded-md border border-slate-300 px-3 py-2 text-sm text-slate-950 outline-none transition focus:border-slate-950"
                />
              </label>

              {editingId && (
                <label className="mb-4 block">
                  <span className="mb-1 block text-sm font-medium text-slate-700">Статус</span>
                  <select
                    value={form.status}
                    onChange={(event) =>
                      setForm((current) => ({ ...current, status: event.target.value as ProjectStatus }))
                    }
                    className="h-10 w-full rounded-md border border-slate-300 px-3 text-sm text-slate-950 outline-none transition focus:border-slate-950"
                  >
                    {statuses.map((status) => (
                      <option key={status} value={status}>
                        {status}
                      </option>
                    ))}
                  </select>
                </label>
              )}

              <div className="flex flex-wrap gap-2">
                <button
                  type="submit"
                  disabled={saving}
                  className="inline-flex h-10 items-center justify-center rounded-md bg-slate-950 px-4 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:bg-slate-300"
                >
                  {saving ? "Сохранение..." : editingId ? "Сохранить" : "Создать"}
                </button>
                {editingId && (
                  <button
                    type="button"
                    onClick={resetForm}
                    className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
                  >
                    Отмена
                  </button>
                )}
              </div>
            </form>

            <section>
              <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
                <div>
                  <p className="text-sm font-semibold uppercase tracking-[0.18em] text-slate-500">
                    Resource server
                  </p>
                  <h2 className="mt-2 text-3xl font-semibold text-slate-950">Проекты</h2>
                </div>
                <button
                  type="button"
                  onClick={loadProjects}
                  disabled={loading}
                  className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 bg-white px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Обновить
                </button>
              </div>

              {notice && (
                <p className="mb-4 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
                  {notice}
                </p>
              )}

              {error && (
                <p role="alert" className="mb-4 rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                  {error}
                </p>
              )}

              <div className="rounded-lg border border-slate-200 bg-white shadow-sm">
                {loading ? (
                  <div className="p-6 text-sm text-slate-600">Загрузка...</div>
                ) : projects.length === 0 ? (
                  <div className="p-6 text-sm text-slate-600">Проектов пока нет.</div>
                ) : (
                  <div className="divide-y divide-slate-100">
                    {projects.map((project) => (
                      <article key={project.id} className="p-5">
                        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                          <div className="min-w-0">
                            <div className="mb-2 flex flex-wrap items-center gap-2">
                              <h3 className="text-lg font-semibold text-slate-950">{project.name}</h3>
                              <span className="rounded-md bg-slate-100 px-2 py-1 text-xs font-semibold text-slate-600">
                                {project.status}
                              </span>
                            </div>
                            <p className="text-sm leading-6 text-slate-600">
                              {project.description || "Описание не заполнено"}
                            </p>
                            <p className="mt-3 break-all font-mono text-xs text-slate-400">
                              owner: {project.ownerId}
                            </p>
                          </div>

                          <div className="flex shrink-0 flex-wrap gap-2">
                            <button
                              type="button"
                              onClick={() => startEdit(project)}
                              className="inline-flex h-9 items-center justify-center rounded-md border border-slate-300 px-3 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
                            >
                              Изменить
                            </button>
                            <button
                              type="button"
                              onClick={() => handleDelete(project.id)}
                              disabled={saving}
                              className="inline-flex h-9 items-center justify-center rounded-md bg-rose-600 px-3 text-xs font-semibold text-white transition hover:bg-rose-500 disabled:cursor-not-allowed disabled:bg-rose-300"
                            >
                              Удалить
                            </button>
                          </div>
                        </div>
                      </article>
                    ))}
                  </div>
                )}
              </div>
            </section>
          </section>
        </main>
      )}
    </RoleGate>
  );
}
