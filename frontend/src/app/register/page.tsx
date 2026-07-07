import { RegisterForm } from "@/features/auth/register";

export default function RegisterPage() {
  return (
    <main className="min-h-[calc(100vh-4rem)] px-4 py-10">
      <section className="mx-auto grid w-full max-w-4xl gap-8 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm md:grid-cols-[0.85fr_1.15fr] md:p-8">
        <div className="rounded-xl bg-slate-950 p-6 text-white">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-300">
            Регистрация
          </p>
          <h1 className="mt-3 text-3xl font-semibold">Создайте аккаунт</h1>
          <p className="mt-3 text-sm leading-6 text-slate-300">
            После регистрации можно войти и проверить работу refresh, ролей и закрытых страниц.
          </p>
        </div>
        <RegisterForm />
      </section>
    </main>
  );
}
