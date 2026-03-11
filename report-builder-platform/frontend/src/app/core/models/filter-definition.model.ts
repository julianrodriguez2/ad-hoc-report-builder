export interface FilterDefinition {
  fieldName: string;
  operator: string;
  value: string | number | boolean | Array<string | number | boolean>;
}
