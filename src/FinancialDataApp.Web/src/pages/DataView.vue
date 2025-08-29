<template>
  <div class="data-view">
    <h1>Data View</h1>

    <!-- Filters Section -->
    <div class="filters-section">
      <div class="filter-row">
        <div class="filter-item">
          <label>Data Source</label>
          <select
            v-model="filters.dataSourceId"
            @change="loadData"
            class="form-control"
          >
            <option value="">All Sources</option>
            <option
              v-for="source in dataSources"
              :key="source.id"
              :value="source.id"
            >
              {{ source.name }}
            </option>
          </select>
        </div>
        <div class="filter-item">
          <label>Date Range</label>
          <div class="date-range">
            <input
              v-model="filters.startDate"
              type="datetime-local"
              @change="loadData"
              class="form-control"
            />
            <span>to</span>
            <input
              v-model="filters.endDate"
              type="datetime-local"
              @change="loadData"
              class="form-control"
            />
          </div>
        </div>
        <div class="filter-item">
          <label>Search</label>
          <input
            v-model="filters.searchTerm"
            type="text"
            placeholder="Search in data..."
            @input="handleSearch"
            class="form-control"
          />
        </div>
      </div>

      <!-- Quick Filters -->
      <div class="quick-filters">
        <button @click="setQuickFilter('today')" class="btn btn-sm">
          Today
        </button>
        <button @click="setQuickFilter('week')" class="btn btn-sm">
          Last Week
        </button>
        <button @click="setQuickFilter('month')" class="btn btn-sm">
          Last Month
        </button>
        <button @click="clearFilters" class="btn btn-sm btn-secondary">
          Clear
        </button>
      </div>
    </div>

    <!-- Export Controls -->
    <div class="export-section">
      <h3>Export Data</h3>
      <div class="export-controls">
        <select v-model="exportFormat" class="form-control">
          <option value="csv">CSV</option>
          <option value="excel">Excel</option>
          <option value="json">JSON</option>
        </select>
        <button
          @click="exportData"
          class="btn btn-primary"
          :disabled="loading || dataItems.length === 0"
        >
          Export {{ dataItems.length }} Records
        </button>
      </div>
    </div>

    <!-- Data Table -->
    <div class="data-table-container">
      <div class="table-header">
        <h3>Data Records</h3>
        <span class="record-count">{{ totalCount }} total records</span>
      </div>

      <table class="data-table">
        <thead>
          <tr>
            <th @click="sortBy('timestamp')" class="sortable">
              Timestamp
              <span class="sort-icon" v-if="filters.sortBy === 'timestamp'">
                {{ filters.sortOrder === "asc" ? "▲" : "▼" }}
              </span>
            </th>
            <th @click="sortBy('dataSourceName')" class="sortable">
              Source
              <span
                class="sort-icon"
                v-if="filters.sortBy === 'dataSourceName'"
              >
                {{ filters.sortOrder === "asc" ? "▲" : "▼" }}
              </span>
            </th>
            <th @click="sortBy('dataType')" class="sortable">
              Type
              <span class="sort-icon" v-if="filters.sortBy === 'dataType'">
                {{ filters.sortOrder === "asc" ? "▲" : "▼" }}
              </span>
            </th>
            <th @click="sortBy('value')" class="sortable">
              Value
              <span class="sort-icon" v-if="filters.sortBy === 'value'">
                {{ filters.sortOrder === "asc" ? "▲" : "▼" }}
              </span>
            </th>
            <th>Metadata</th>
            <th>Dynamic Fields</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="7" class="text-center">Loading...</td>
          </tr>
          <tr v-else-if="dataItems.length === 0">
            <td colspan="7" class="text-center">No data found</td>
          </tr>
          <tr v-for="item in dataItems" :key="item.id">
            <td>{{ formatTimestamp(item.timestamp) }}</td>
            <td>{{ item.dataSourceName || "Unknown" }}</td>
            <td>
              <span class="badge">{{ item.dataType }}</span>
            </td>
            <td class="value-cell">{{ formatValue(item.value) }}</td>
            <td>
              <button @click="showMetadata(item)" class="btn btn-sm">
                View
              </button>
            </td>
            <td>
              <button
                v-if="
                  item.dynamicFields &&
                  Object.keys(item.dynamicFields).length > 0
                "
                @click="showDynamicFields(item)"
                class="btn btn-sm"
              >
                {{ Object.keys(item.dynamicFields).length }} fields
              </button>
              <span v-else class="text-muted">None</span>
            </td>
            <td>
              <button @click="viewDetails(item)" class="btn btn-sm">
                Details
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Pagination -->
      <div class="pagination">
        <button
          @click="changePage(currentPage - 1)"
          :disabled="currentPage === 1"
          class="btn btn-sm"
        >
          Previous
        </button>
        <span class="page-info">
          Page {{ currentPage }} of {{ totalPages }}
        </span>
        <select
          v-model="pageSize"
          @change="changePageSize"
          class="page-size-select"
        >
          <option value="10">10 per page</option>
          <option value="25">25 per page</option>
          <option value="50">50 per page</option>
          <option value="100">100 per page</option>
        </select>
        <button
          @click="changePage(currentPage + 1)"
          :disabled="currentPage === totalPages"
          class="btn btn-sm"
        >
          Next
        </button>
      </div>
    </div>

    <!-- Metadata Modal -->
    <div
      v-if="showMetadataModal"
      class="modal-overlay"
      @click.self="closeMetadataModal"
    >
      <div class="modal">
        <div class="modal-header">
          <h2>Metadata</h2>
          <button @click="closeMetadataModal" class="close-btn">&times;</button>
        </div>
        <div class="modal-body">
          <pre class="json-display">{{
            JSON.stringify(selectedItem?.metadata, null, 2)
          }}</pre>
        </div>
      </div>
    </div>

    <!-- Dynamic Fields Modal -->
    <div
      v-if="showDynamicFieldsModal"
      class="modal-overlay"
      @click.self="closeDynamicFieldsModal"
    >
      <div class="modal">
        <div class="modal-header">
          <h2>Dynamic Fields</h2>
          <button @click="closeDynamicFieldsModal" class="close-btn">
            &times;
          </button>
        </div>
        <div class="modal-body">
          <div
            v-for="(value, key) in selectedItem?.dynamicFields"
            :key="key"
            class="field-item"
          >
            <strong>{{ key }}:</strong> {{ value }}
          </div>
        </div>
      </div>
    </div>

    <!-- Details Modal -->
    <div
      v-if="showDetailsModal"
      class="modal-overlay"
      @click.self="closeDetailsModal"
    >
      <div class="modal modal-lg">
        <div class="modal-header">
          <h2>Data Record Details</h2>
          <button @click="closeDetailsModal" class="close-btn">&times;</button>
        </div>
        <div class="modal-body">
          <div class="detail-grid">
            <div class="detail-item">
              <label>ID:</label>
              <span>{{ selectedItem?.id }}</span>
            </div>
            <div class="detail-item">
              <label>Timestamp:</label>
              <span>{{ formatTimestamp(selectedItem?.timestamp) }}</span>
            </div>
            <div class="detail-item">
              <label>Data Source:</label>
              <span
                >{{ selectedItem?.dataSourceName }} (ID:
                {{ selectedItem?.dataSourceId }})</span
              >
            </div>
            <div class="detail-item">
              <label>Type:</label>
              <span>{{ selectedItem?.dataType }}</span>
            </div>
            <div class="detail-item">
              <label>Value:</label>
              <span>{{ selectedItem?.value }}</span>
            </div>
          </div>

          <div class="detail-section">
            <h4>Metadata</h4>
            <pre class="json-display">{{
              JSON.stringify(selectedItem?.metadata, null, 2)
            }}</pre>
          </div>

          <div
            v-if="
              selectedItem?.dynamicFields &&
              Object.keys(selectedItem.dynamicFields).length > 0
            "
            class="detail-section"
          >
            <h4>Dynamic Fields</h4>
            <div
              v-for="(value, key) in selectedItem.dynamicFields"
              :key="key"
              class="field-item"
            >
              <strong>{{ key }}:</strong> {{ value }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import api from "../services/api";
import type {
  RealTimeData,
  DataSource,
  QueryParams,
  ExportOptions,
} from "../types";

// Reactive data
const dataItems = ref<RealTimeData[]>([]);
const dataSources = ref<DataSource[]>([]);
const loading = ref(false);
const currentPage = ref(1);
const pageSize = ref(25);
const totalCount = ref(0);
const exportFormat = ref<"csv" | "excel" | "json">("csv");

// Filters
const filters = ref<QueryParams>({
  dataSourceId: "",
  searchTerm: "",
  startDate: "",
  endDate: "",
  sortBy: "timestamp",
  sortOrder: "desc",
});

// Modal states
const showMetadataModal = ref(false);
const showDynamicFieldsModal = ref(false);
const showDetailsModal = ref(false);
const selectedItem = ref<RealTimeData | null>(null);

// Computed
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value));

// Methods
const loadData = async () => {
  loading.value = true;
  try {
    const params: QueryParams = {
      pageNumber: currentPage.value,
      pageSize: pageSize.value,
      ...filters.value,
    };

    // Remove empty values
    Object.keys(params).forEach((key) => {
      if (
        params[key] === "" ||
        params[key] === null ||
        params[key] === undefined
      ) {
        delete params[key];
      }
    });

    const response = await api.realTimeData.getAll(params);
    dataItems.value = response.items;
    totalCount.value = response.totalCount;
  } catch (error) {
    console.error("Failed to load data:", error);
  } finally {
    loading.value = false;
  }
};

const loadDataSources = async () => {
  try {
    const response = await api.dataSources.getAll({ pageSize: 100 });
    dataSources.value = response.items;
  } catch (error) {
    console.error("Failed to load data sources:", error);
  }
};

const handleSearch = () => {
  currentPage.value = 1;
  loadData();
};

const sortBy = (field: string) => {
  if (filters.value.sortBy === field) {
    filters.value.sortOrder =
      filters.value.sortOrder === "asc" ? "desc" : "asc";
  } else {
    filters.value.sortBy = field;
    filters.value.sortOrder = "asc";
  }
  loadData();
};

const changePage = (page: number) => {
  currentPage.value = page;
  loadData();
};

const changePageSize = () => {
  currentPage.value = 1;
  loadData();
};

const setQuickFilter = (period: "today" | "week" | "month") => {
  const now = new Date();
  const endDate = now.toISOString().slice(0, 16);
  let startDate: string;

  switch (period) {
    case "today":
      startDate = new Date(now.setHours(0, 0, 0, 0)).toISOString().slice(0, 16);
      break;
    case "week":
      startDate = new Date(now.setDate(now.getDate() - 7))
        .toISOString()
        .slice(0, 16);
      break;
    case "month":
      startDate = new Date(now.setMonth(now.getMonth() - 1))
        .toISOString()
        .slice(0, 16);
      break;
  }

  filters.value.startDate = startDate;
  filters.value.endDate = endDate;
  loadData();
};

const clearFilters = () => {
  filters.value = {
    dataSourceId: "",
    searchTerm: "",
    startDate: "",
    endDate: "",
    sortBy: "timestamp",
    sortOrder: "desc",
  };
  loadData();
};

const exportData = async () => {
  try {
    const options: ExportOptions = {
      format: exportFormat.value,
      includeHeaders: true,
    };

    if (filters.value.startDate && filters.value.endDate) {
      options.dateRange = {
        start: filters.value.startDate,
        end: filters.value.endDate,
      };
    }

    const blob = await api.realTimeData.export(options);
    const filename = `data-export-${new Date().toISOString().slice(0, 10)}.${
      exportFormat.value
    }`;
    api.export.downloadBlob(blob, filename);
  } catch (error) {
    console.error("Failed to export data:", error);
    alert("Failed to export data");
  }
};

const formatTimestamp = (timestamp?: string) => {
  if (!timestamp) return "";
  return new Date(timestamp).toLocaleString();
};

const formatValue = (value: number) => {
  return value.toLocaleString(undefined, { maximumFractionDigits: 2 });
};

const showMetadata = (item: RealTimeData) => {
  selectedItem.value = item;
  showMetadataModal.value = true;
};

const closeMetadataModal = () => {
  showMetadataModal.value = false;
  selectedItem.value = null;
};

const showDynamicFields = (item: RealTimeData) => {
  selectedItem.value = item;
  showDynamicFieldsModal.value = true;
};

const closeDynamicFieldsModal = () => {
  showDynamicFieldsModal.value = false;
  selectedItem.value = null;
};

const viewDetails = (item: RealTimeData) => {
  selectedItem.value = item;
  showDetailsModal.value = true;
};

const closeDetailsModal = () => {
  showDetailsModal.value = false;
  selectedItem.value = null;
};

// Lifecycle
onMounted(() => {
  loadDataSources();
  loadData();
});
</script>

<style scoped>
.data-view {
  padding: 20px;
}

.filters-section {
  background: white;
  padding: 20px;
  border-radius: 8px;
  margin-bottom: 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.filter-row {
  display: grid;
  grid-template-columns: 1fr 2fr 1fr;
  gap: 20px;
  margin-bottom: 15px;
}

.filter-item label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
  font-size: 14px;
}

.date-range {
  display: flex;
  align-items: center;
  gap: 10px;
}

.quick-filters {
  display: flex;
  gap: 10px;
}

.export-section {
  background: white;
  padding: 20px;
  border-radius: 8px;
  margin-bottom: 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.export-section h3 {
  margin: 0 0 15px 0;
  font-size: 16px;
}

.export-controls {
  display: flex;
  gap: 10px;
  align-items: center;
}

.export-controls .form-control {
  width: 150px;
}

.data-table-container {
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #e0e0e0;
}

.table-header h3 {
  margin: 0;
}

.record-count {
  color: #666;
  font-size: 14px;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
}

.data-table th {
  background: #f5f5f5;
  padding: 12px;
  text-align: left;
  font-weight: 600;
  border-bottom: 2px solid #e0e0e0;
}

.data-table th.sortable {
  cursor: pointer;
  user-select: none;
}

.data-table th.sortable:hover {
  background: #ebebeb;
}

.sort-icon {
  margin-left: 5px;
  font-size: 12px;
}

.data-table td {
  padding: 12px;
  border-bottom: 1px solid #f0f0f0;
}

.data-table tr:hover {
  background: #f9f9f9;
}

.value-cell {
  font-weight: 600;
  color: #2c3e50;
}

.badge {
  display: inline-block;
  padding: 4px 8px;
  background: #e3f2fd;
  color: #1976d2;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
}

.text-muted {
  color: #999;
  font-size: 14px;
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 10px;
  padding: 20px;
}

.page-size-select {
  padding: 4px 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 12px;
}

/* Form Controls */
.form-control {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
}

.form-control:focus {
  outline: none;
  border-color: #42b883;
}

/* Buttons */
.btn {
  padding: 8px 16px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.2s;
}

.btn-sm {
  padding: 4px 8px;
  font-size: 12px;
}

.btn-primary {
  background: #42b883;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #359268;
}

.btn-primary:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.btn-secondary {
  background: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background: #5a6268;
}

/* Modal Styles */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal {
  background: white;
  border-radius: 8px;
  width: 90%;
  max-width: 500px;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-lg {
  max-width: 800px;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #e0e0e0;
}

.modal-header h2 {
  margin: 0;
}

.close-btn {
  background: none;
  border: none;
  font-size: 24px;
  cursor: pointer;
  color: #666;
}

.modal-body {
  padding: 20px;
}

.json-display {
  background: #f5f5f5;
  padding: 15px;
  border-radius: 4px;
  overflow-x: auto;
  font-family: monospace;
  font-size: 12px;
}

.field-item {
  padding: 8px 0;
  border-bottom: 1px solid #f0f0f0;
}

.field-item:last-child {
  border-bottom: none;
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 15px;
  margin-bottom: 20px;
}

.detail-item label {
  font-weight: 600;
  margin-right: 10px;
}

.detail-section {
  margin-top: 20px;
}

.detail-section h4 {
  margin: 0 0 10px 0;
  font-size: 16px;
  color: #333;
}

.text-center {
  text-align: center;
}
</style>
