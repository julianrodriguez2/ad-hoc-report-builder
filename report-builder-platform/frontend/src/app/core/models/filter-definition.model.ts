export interface NumberRangeValue {
  min: number | null;
  max: number | null;
}

export interface DateRangeValue {
  start: string | null;
  end: string | null;
}

export type FilterValue =
  | string
  | number
  | boolean
  | null
  | NumberRangeValue
  | DateRangeValue;

export interface FilterDefinition {
  id: string;
  fieldName: string;
  displayName?: string;
  dataType: string;
  operator: string;
  value: FilterValue;
}
