export function csvText(rows: Record<string, unknown>[], context: Record<string, unknown>): string {
  const cells = (values: unknown[]) => values.map(value => {
    let text = value == null ? '' : typeof value === 'object' ? JSON.stringify(value) : String(value);
    if (/^[\s]*[=+@-]/.test(text) && typeof value !== 'number') text = "'" + text;
    return '"' + text.replaceAll('"', '""') + '"';
  }).join(',');
  const columns = [...new Set(rows.flatMap(row => Object.keys(row)))];
  return '\ufeff' + [...Object.entries(context).map(cells), '', cells(columns),
    ...rows.map(row => cells(columns.map(key => row[key])))].join('\r\n');
}
export function downloadCsv(name: string, rows: Record<string, unknown>[], context: Record<string, unknown>): void {
  const url = URL.createObjectURL(new Blob([csvText(rows, context)], { type: 'text/csv;charset=utf-8' }));
  const anchor = document.createElement('a'); anchor.href = url; anchor.download = name; anchor.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
