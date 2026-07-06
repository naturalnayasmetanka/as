"use client";

import { RoleGate } from "@/shared/auth";

export default function StaffPage() {
  return (
    <RoleGate roles={["Employee", "Admin"]}>
      <main className="mx-auto w-full max-w-6xl px-4 py-8">
        <h1 className="text-2xl font-semibold text-slate-950">Staff</h1>
        <div className="mt-5 border border-slate-200 bg-white p-6">
          <p className="text-sm text-slate-600">Внутренний staff-раздел.</p>
        </div>
      </main>
    </RoleGate>
  );
}
