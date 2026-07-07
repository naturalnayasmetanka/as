import Link from "next/link";

export function ForbiddenScreen() {
  return (
    <main className="min-h-[calc(100vh-4rem)] px-4 py-10">
      <section className="mx-auto max-w-xl rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
        <p className="text-sm font-semibold uppercase tracking-[0.2em] text-rose-600">403</p>
        <h1 className="mt-3 text-3xl font-semibold text-slate-950">Доступ закрыт</h1>
        <p className="mt-3 text-sm leading-6 text-slate-600">
          У вашей текущей сессии нет роли для просмотра этой страницы.
        </p>
        <Link
          href="/profile"
          className="mt-6 inline-flex h-11 items-center justify-center rounded-lg bg-slate-950 px-4 text-sm font-semibold text-white transition hover:bg-slate-800"
        >
          В профиль
        </Link>
      </section>
    </main>
  );
}
