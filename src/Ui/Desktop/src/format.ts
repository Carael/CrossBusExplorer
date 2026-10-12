const countFormat = new Intl.NumberFormat("de-CH");

export function formatCount(value: number) {
  return countFormat.format(value);
}
