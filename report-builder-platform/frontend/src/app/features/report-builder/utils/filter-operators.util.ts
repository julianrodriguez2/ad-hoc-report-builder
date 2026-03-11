import { DateRangeValue, FilterValue, NumberRangeValue } from '../../../core/models/filter-definition.model';

export type FilterOperator =
  | 'equals'
  | 'notEquals'
  | 'contains'
  | 'startsWith'
  | 'endsWith'
  | 'isBlank'
  | 'isNotBlank'
  | 'greaterThan'
  | 'greaterThanOrEqual'
  | 'lessThan'
  | 'lessThanOrEqual'
  | 'between'
  | 'isNull'
  | 'isNotNull'
  | 'before'
  | 'after';

export type NormalizedFieldType = 'string' | 'number' | 'date' | 'boolean';

export interface FilterOperatorOption {
  label: string;
  value: FilterOperator;
}

const OPERATOR_LABELS: Record<FilterOperator, string> = {
  equals: 'Equals',
  notEquals: 'Not Equals',
  contains: 'Contains',
  startsWith: 'Starts With',
  endsWith: 'Ends With',
  isBlank: 'Is Blank',
  isNotBlank: 'Is Not Blank',
  greaterThan: 'Greater Than',
  greaterThanOrEqual: 'Greater Than or Equal',
  lessThan: 'Less Than',
  lessThanOrEqual: 'Less Than or Equal',
  between: 'Between',
  isNull: 'Is Null',
  isNotNull: 'Is Not Null',
  before: 'Before',
  after: 'After'
};

const OPERATORS_BY_DATA_TYPE: Record<NormalizedFieldType, FilterOperator[]> = {
  string: ['equals', 'notEquals', 'contains', 'startsWith', 'endsWith', 'isBlank', 'isNotBlank'],
  number: ['equals', 'notEquals', 'greaterThan', 'greaterThanOrEqual', 'lessThan', 'lessThanOrEqual', 'between', 'isNull', 'isNotNull'],
  date: ['equals', 'before', 'after', 'between', 'isNull', 'isNotNull'],
  boolean: ['equals']
};

const NO_VALUE_OPERATORS = new Set<FilterOperator>(['isBlank', 'isNotBlank', 'isNull', 'isNotNull']);
const BETWEEN_OPERATOR: FilterOperator = 'between';

export function normalizeDataType(dataType: string | null | undefined): NormalizedFieldType {
  const normalized = (dataType ?? '').trim().toLowerCase();

  if (!normalized) {
    return 'string';
  }

  if (normalized.includes('bool')) {
    return 'boolean';
  }

  if (normalized.includes('date') || normalized.includes('time')) {
    return 'date';
  }

  const numberTokens = ['number', 'numeric', 'int', 'decimal', 'double', 'float', 'long', 'short'];
  if (numberTokens.some((token) => normalized.includes(token))) {
    return 'number';
  }

  return 'string';
}

export function getOperatorsForDataType(dataType: string): FilterOperatorOption[] {
  const type = normalizeDataType(dataType);
  return OPERATORS_BY_DATA_TYPE[type].map((operator) => ({
    label: OPERATOR_LABELS[operator],
    value: operator
  }));
}

export function getDefaultOperator(dataType: string): FilterOperator {
  const type = normalizeDataType(dataType);
  return OPERATORS_BY_DATA_TYPE[type][0];
}

export function requiresValue(operator: string): boolean {
  if (!operator) {
    return false;
  }

  return !NO_VALUE_OPERATORS.has(operator as FilterOperator);
}

export function isBetweenOperator(operator: string): boolean {
  return operator === BETWEEN_OPERATOR;
}

export function createDefaultValue(dataType: string, operator: string): FilterValue {
  if (!requiresValue(operator)) {
    return null;
  }

  if (isBetweenOperator(operator)) {
    if (normalizeDataType(dataType) === 'date') {
      const emptyDateRange: DateRangeValue = { start: null, end: null };
      return emptyDateRange;
    }

    const emptyNumberRange: NumberRangeValue = { min: null, max: null };
    return emptyNumberRange;
  }

  const type = normalizeDataType(dataType);
  if (type === 'boolean') {
    return true;
  }

  return null;
}
