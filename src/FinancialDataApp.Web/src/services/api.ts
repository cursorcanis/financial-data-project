import axios, { AxiosInstance, AxiosError } from 'axios';
import type {
  User,
  DataSource,
  RealTimeData,
  SystemHealth,
  AuditLog,
  DynamicField,
  ApiResponse,
  PaginatedResponse,
  QueryParams,
  ExportOptions
} from '../types';

// Create axios instance with default configuration
const apiClient: AxiosInstance = axios.create({
  baseURL: '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor for authentication
apiClient.interceptors.request.use(
  (config) => {
    // Add auth token if available
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Redirect to login or refresh token
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// User API methods
export const userApi = {
  async getAll(params?: QueryParams): Promise<PaginatedResponse<User>> {
    const response = await apiClient.get('/users', { params });
    return response.data;
  },

  async getById(id: number): Promise<User> {
    const response = await apiClient.get(`/users/${id}`);
    return response.data;
  },

  async create(user: Partial<User>): Promise<User> {
    const response = await apiClient.post('/users', user);
    return response.data;
  },

  async update(id: number, user: Partial<User>): Promise<User> {
    const response = await apiClient.put(`/users/${id}`, user);
    return response.data;
  },

  async delete(id: number): Promise<void> {
    await apiClient.delete(`/users/${id}`);
  },

  async login(credentials: { username: string; password: string }): Promise<{ token: string; user: User }> {
    const response = await apiClient.post('/auth/login', credentials);
    return response.data;
  },

  async logout(): Promise<void> {
    await apiClient.post('/auth/logout');
    localStorage.removeItem('authToken');
  },
};

// Data Source API methods
export const dataSourceApi = {
  async getAll(params?: QueryParams): Promise<PaginatedResponse<DataSource>> {
    const response = await apiClient.get('/datasources', { params });
    return response.data;
  },

  async getById(id: number): Promise<DataSource> {
    const response = await apiClient.get(`/datasources/${id}`);
    return response.data;
  },

  async create(dataSource: Partial<DataSource>): Promise<DataSource> {
    const response = await apiClient.post('/datasources', dataSource);
    return response.data;
  },

  async update(id: number, dataSource: Partial<DataSource>): Promise<DataSource> {
    const response = await apiClient.put(`/datasources/${id}`, dataSource);
    return response.data;
  },

  async delete(id: number): Promise<void> {
    await apiClient.delete(`/datasources/${id}`);
  },

  async testConnection(id: number): Promise<ApiResponse<boolean>> {
    const response = await apiClient.post(`/datasources/${id}/test`);
    return response.data;
  },

  async sync(id: number): Promise<ApiResponse<void>> {
    const response = await apiClient.post(`/datasources/${id}/sync`);
    return response.data;
  },
};

// Real-time Data API methods
export const realTimeDataApi = {
  async getAll(params?: QueryParams): Promise<PaginatedResponse<RealTimeData>> {
    const response = await apiClient.get('/realtimedata', { params });
    return response.data;
  },

  async getById(id: number): Promise<RealTimeData> {
    const response = await apiClient.get(`/realtimedata/${id}`);
    return response.data;
  },

  async getByDataSource(dataSourceId: number, params?: QueryParams): Promise<PaginatedResponse<RealTimeData>> {
    const response = await apiClient.get(`/realtimedata/datasource/${dataSourceId}`, { params });
    return response.data;
  },

  async getLatest(limit: number = 100): Promise<RealTimeData[]> {
    const response = await apiClient.get(`/realtimedata/latest/${limit}`);
    return response.data;
  },

  async export(options: ExportOptions): Promise<Blob> {
    const response = await apiClient.post('/realtimedata/export', options, {
      responseType: 'blob',
    });
    return response.data;
  },

  async getStatistics(dataSourceId?: number, dateRange?: { start: string; end: string }): Promise<any> {
    const response = await apiClient.get('/realtimedata/statistics', {
      params: { dataSourceId, ...dateRange },
    });
    return response.data;
  },
};

// System Health API methods
export const healthApi = {
  async getAll(params?: QueryParams): Promise<PaginatedResponse<SystemHealth>> {
    const response = await apiClient.get('/health', { params });
    return response.data;
  },

  async getCurrent(): Promise<SystemHealth[]> {
    const response = await apiClient.get('/health/current');
    return response.data;
  },

  async getByComponent(component: string, params?: QueryParams): Promise<PaginatedResponse<SystemHealth>> {
    const response = await apiClient.get(`/health/component/${component}`, { params });
    return response.data;
  },

  async getMetrics(period: 'hour' | 'day' | 'week' | 'month'): Promise<any> {
    const response = await apiClient.get(`/health/metrics/${period}`);
    return response.data;
  },

  async runHealthCheck(): Promise<ApiResponse<SystemHealth[]>> {
    const response = await apiClient.post('/health/check');
    return response.data;
  },
};

// Audit Log API methods
export const auditApi = {
  async getAll(params?: QueryParams): Promise<PaginatedResponse<AuditLog>> {
    const response = await apiClient.get('/audit', { params });
    return response.data;
  },

  async getByUser(userId: number, params?: QueryParams): Promise<PaginatedResponse<AuditLog>> {
    const response = await apiClient.get(`/audit/user/${userId}`, { params });
    return response.data;
  },

  async getByEntity(entity: string, entityId: number): Promise<AuditLog[]> {
    const response = await apiClient.get(`/audit/entity/${entity}/${entityId}`);
    return response.data;
  },

  async export(options: ExportOptions): Promise<Blob> {
    const response = await apiClient.post('/audit/export', options, {
      responseType: 'blob',
    });
    return response.data;
  },
};

// Dynamic Fields API methods
export const dynamicFieldApi = {
  async getAll(params?: QueryParams): Promise<PaginatedResponse<DynamicField>> {
    const response = await apiClient.get('/dynamicfields', { params });
    return response.data;
  },

  async getById(id: number): Promise<DynamicField> {
    const response = await apiClient.get(`/dynamicfields/${id}`);
    return response.data;
  },

  async getByEntity(entityType: string): Promise<DynamicField[]> {
    const response = await apiClient.get(`/dynamicfields/entity/${entityType}`);
    return response.data;
  },

  async create(field: Partial<DynamicField>): Promise<DynamicField> {
    const response = await apiClient.post('/dynamicfields', field);
    return response.data;
  },

  async update(id: number, field: Partial<DynamicField>): Promise<DynamicField> {
    const response = await apiClient.put(`/dynamicfields/${id}`, field);
    return response.data;
  },

  async delete(id: number): Promise<void> {
    await apiClient.delete(`/dynamicfields/${id}`);
  },

  async validateField(field: DynamicField, value: any): Promise<ApiResponse<boolean>> {
    const response = await apiClient.post('/dynamicfields/validate', { field, value });
    return response.data;
  },
};

// Export service for general exports
export const exportApi = {
  async exportData(entity: string, options: ExportOptions): Promise<Blob> {
    const response = await apiClient.post(`/export/${entity}`, options, {
      responseType: 'blob',
    });
    return response.data;
  },

  async downloadFile(url: string): Promise<Blob> {
    const response = await apiClient.get(url, {
      responseType: 'blob',
    });
    return response.data;
  },

  // Helper function to trigger download
  downloadBlob(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  },
};

// Export all API services
export default {
  users: userApi,
  dataSources: dataSourceApi,
  realTimeData: realTimeDataApi,
  health: healthApi,
  audit: auditApi,
  dynamicFields: dynamicFieldApi,
  export: exportApi,
};
