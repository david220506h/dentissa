/** Une nombres de clase e ignora los valores falsos. */
export function clases(...valores: Array<string | false | null | undefined>): string {
  return valores.filter(Boolean).join(' ');
}
