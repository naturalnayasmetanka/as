import Link from "next/link";
import { RoleGate } from "@/shared/session/ui";

const adminCards = [
  ["Пользователи", "Список аккаунтов, текущие роли и назначение прав доступа.", "/admin/users"],
  ["Политики доступа", "Роли преобразуются в permissions на сервере. JWT хранит только роли.", null],
  ["Аудит", "Будущая зона для журнала административных действий.", null],
];

export default function AdminPage() {
  return (
    <RoleGate allowedRoles={["Admin"]}>
      <main className="min-h-[calc(100vh-4rem)] px-4 py-10">
        <section className="mx-auto max-w-6xl">
          <div className="rounded-2xl border border-slate-200 bg-white p-8 shadow-sm sm:p-10">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-500">
              Admin
            </p>
            <div className="mt-3 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
              <div>
                <h1 className="text-4xl font-semibold tracking-tight text-slate-950">
                  Панель администратора
                </h1>
                <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-600">
                  Управление пользователями и проверка серверной авторизации через permissions.
                </p>
              </div>
              <Link
                href="/admin/users"
                className="inline-flex h-11 items-center justify-center rounded-lg bg-slate-950 px-4 text-sm font-semibold text-white transition hover:bg-slate-800"
              >
                Открыть пользователей
              </Link>
            </div>
          </div>

          <div className="mt-6 grid gap-4 md:grid-cols-3">
            {adminCards.map(([title, description, href]) => (
              <article
                key={title}
                className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
              >
                <h2 className="text-lg font-semibold text-slate-950">{title}</h2>
                <p className="mt-2 min-h-16 text-sm leading-6 text-slate-600">{description}</p>
                {href ? (
                  <Link
                    href={href}
                    className="mt-5 inline-flex h-10 items-center justify-center rounded-lg border border-slate-300 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
                  >
                    Перейти
                  </Link>
                ) : (
                  <p className="mt-5 inline-flex h-10 items-center rounded-lg bg-slate-100 px-4 text-sm font-semibold text-slate-500">
                    Скоро
                  </p>
                )}
              </article>
            ))}
          </div>
        </section>
      </main>
    </RoleGate>
  );
}
