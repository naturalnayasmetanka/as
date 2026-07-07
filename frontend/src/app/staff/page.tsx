import { RoleGate } from "@/shared/session/ui";

const stats = [
  ["12", "внутренних задач"],
  ["4", "активных процесса"],
  ["98%", "стабильность сессий"],
];

export default function StaffPage() {
  return (
    <RoleGate allowedRoles={["Employee", "Moderator", "Admin"]}>
      <main className="min-h-[calc(100vh-4rem)] px-4 py-10">
        <section className="mx-auto max-w-6xl">
          <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
            <div className="grid gap-0 md:grid-cols-[1.1fr_0.9fr]">
              <div className="bg-slate-950 p-8 text-white sm:p-10">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-emerald-300">
                  Staff
                </p>
                <h1 className="mt-4 max-w-xl text-4xl font-semibold tracking-tight">
                  Рабочая зона сотрудников
                </h1>
                <p className="mt-4 max-w-2xl text-sm leading-6 text-slate-300">
                  Здесь будет внутренняя панель: операционные задачи, состояние сервисов,
                  быстрые переходы и рабочие уведомления команды.
                </p>
              </div>

              <div className="grid content-center gap-4 p-8 sm:p-10">
                {stats.map(([value, label]) => (
                  <div key={label} className="rounded-xl border border-slate-200 bg-slate-50 p-5">
                    <p className="text-3xl font-semibold text-slate-950">{value}</p>
                    <p className="mt-1 text-sm text-slate-600">{label}</p>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </section>
      </main>
    </RoleGate>
  );
}
