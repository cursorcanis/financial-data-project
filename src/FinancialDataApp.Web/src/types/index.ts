// User related types
export interface User {
  id: number;
  username: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

// Data Source types
export interface DataSource {
  id: number;
  name: string;
  type: 'API' | 'Database' | 'File' | 'Stream';
  connectionString: string;
  isActive: boolean;
  configuration: Record<string, any>;
  lastSync?: string;
  createdAt: string;
  updatedAt: string;
}

// Real-time data types
export interface RealTimeData {
  id: number;
  dataSourceId: number;
  dataSourceName?: string;
  timestamp: string;
  dataType: string;
  value: number;
  metadata: Record<string, any>;
  dynamicFields?: Record<string, any>;
}

// System Health types
export interface SystemHealth {
  id: number;
  timestamp: string;
  component: string;
  status: 'Healthy' | 'Degraded' | 'Unhealthy';
  cpuUsage: number;
  memoryUsage: number;
  diskUsage: number;
  responseTime: number;
  errorRate: number;
  details?: string;
}

// Audit Log types
export interface AuditLog {
  id: number;
  userId: number;
  username?: string;
  action: string;
  entity: string;
  entityId?: number;
  timestamp: string;
  oldValue?: string;
  newValue?: string;
  ipAddress?: string;
}

// Dynamic Field types
export interface DynamicField {
  id: number;
  fieldName: string;
  fieldType: 'String' | 'Number' | 'Boolean' | 'Date' | 'Json';
  isRequired: boolean;
  defaultValue?: any;
  validationRules?: string;
  description?: string;
  entityType: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

// API Response types
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// Filter and Query types
export interface QueryParams {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  searchTerm?: string;
  startDate?: string;
  endDate?: string;
  [key: string]: any;
}

// Chart data types
export interface ChartData {
  labels: string[];
  datasets: ChartDataset[];
}

export interface ChartDataset {
  label: string;
  data: number[];
  backgroundColor?: string | string[];
  borderColor?: string | string[];
  borderWidth?: number;
}

// Export types
export interface ExportOptions {
  format: 'csv' | 'excel' | 'json';
  includeHeaders?: boolean;
  dateRange?: {
    start: string;
    end: string;
  };
  fields?: string[];
}

// SignalR Hub types
export interface IngestionUpdate {
  timestamp: string;
  rate: number;
  totalRecords: number;
  successCount: number;
  errorCount: number;
}

export interface HealthUpdate {
  component: string;
  status: 'Healthy' | 'Degraded' | 'Unhealthy';
  metrics: {
    cpuUsage: number;
    memoryUsage: number;
    responseTime: number;
  };
}
