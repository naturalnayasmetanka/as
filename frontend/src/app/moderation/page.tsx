import { RoleGate } from "@/shared/session/ui";

const queue = [
  ["Новые жалобы", "Ожидают первичной проверки"],
  ["Спорные действия", "Нужна ручная модерация"],
  ["История решений", "Аудит последних изменений"],
];

export default function ModerationPage() {
  return (
    <RoleGate allowedRoles={["Moderator", "Admin"]}>
      <main className="min-h-[calc(100vh-4rem)] px-4 py-10">
        <section className="mx-auto max-w-6xl">
          <div className="mb-6">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-500">
              Moderation
            </p>
            <h1 className="mt-2 text-3xl font-semibold text-slate-950">Центр модерации</h1>
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            {queue.map(([title, description]) => (
              <article
                key={title}
                className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
              >
                <div className="mb-5 h-2 w-16 rounded-full bg-amber-400" />
                <h2 className="text-lg font-semibold text-slate-950">{title}</h2>
                <p className="mt-2 text-sm leading-6 text-slate-600">{description}</p>
                <div className="mt-6 rounded-xl bg-slate-50 p-4 text-sm text-slate-500">
                  Раздел-заглушка. Позже здесь появятся фильтры, очереди и действия модератора.
                </div>
              </article>
            ))}
          </div>
        </section>
      </main>
    </RoleGate>
  );
}
