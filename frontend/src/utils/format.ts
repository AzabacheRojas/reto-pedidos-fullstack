const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });

export const formatCurrency = (value: number) => currency.format(value);

/** Formatea "2025-01-10T00:00:00" → "10/01/2025" sin conversiones de zona horaria. */
export function formatDate(iso: string): string {
    const [y, m, d] = iso.slice(0, 10).split('-');
    return y && m && d ? `${d}/${m}/${y}` : iso;
}

/** Extrae yyyy-MM-dd para inputs type="date". */
export const toDateInput = (iso: string) => iso.slice(0, 10);

/** Fecha de hoy en formato yyyy-MM-dd según la zona horaria local. */
export function todayInput(): string {
    const now = new Date();
    const offset = now.getTimezoneOffset() * 60_000;
    return new Date(now.getTime() - offset).toISOString().slice(0, 10);
}