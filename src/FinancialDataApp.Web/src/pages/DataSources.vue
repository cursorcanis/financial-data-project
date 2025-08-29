<template>
  <div class="data-sources">
    <div class="page-header">
      <h1>Data Sources</h1>
      <button class="btn btn-primary" @click="showAddModal = true">
        Add Data Source
      </button>
    </div>

    <!-- Search and Filter -->
    <div class="filters">
      <input
        v-model="searchTerm"
        type="text"
        placeholder="Search data sources..."
        class="search-input"
        @input="handleSearch"
      />
      <select
        v-model="filterType"
        @change="loadDataSources"
        class="filter-select"
      >
        <option value="">All Types</option>
        <option value="API">API</option>
        <option value="Database">Database</option>
        <option value="File">File</option>
        <option value="Stream">Stream</option>
      </select>
      <select
        v-model="filterStatus"
        @change="loadDataSources"
        class="filter-select"
      >
        <option value="">All Status</option>
        <option value="true">Active</option>
        <option value="false">Inactive</option>
      </select>
    </div>

    <!-- Data Sources Table -->
    <div class="table-container">
      <table class="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Type</th>
            <th>Status</th>
            <th>Last Sync</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="5" class="text-center">Loading...</td>
          </tr>
          <tr v-else-if="dataSources.length === 0">
            <td colspan="5" class="text-center">No data sources found</td>
          </tr>
          <tr v-for="source in dataSources" :key="source.id">
            <td>{{ source.name }}</td>
            <td>
              <span class="badge" :class="`badge-${source.type.toLowerCase()}`">
                {{ source.type }}
              </span>
            </td>
            <td>
              <span class="status" :class="{ active: source.isActive }">
                {{ source.isActive ? "Active" : "Inactive" }}
              </span>
            </td>
            <td>{{ formatDate(source.lastSync) }}</td>
            <td class="actions">
              <button @click="testConnection(source)" class="btn btn-sm">
                Test
              </button>
              <button @click="syncDataSource(source)" class="btn btn-sm">
                Sync
              </button>
              <button @click="editDataSource(source)" class="btn btn-sm">
                Edit
              </button>
              <button
                @click="deleteDataSource(source)"
                class="btn btn-sm btn-danger"
              >
                Delete
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div class="pagination" v-if="totalPages > 1">
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
      <button
        @click="changePage(currentPage + 1)"
        :disabled="currentPage === totalPages"
        class="btn btn-sm"
      >
        Next
      </button>
    </div>

    <!-- Add/Edit Modal -->
    <div
      v-if="showAddModal || showEditModal"
      class="modal-overlay"
      @click.self="closeModal"
    >
      <div class="modal">
        <div class="modal-header">
          <h2>{{ showEditModal ? "Edit" : "Add" }} Data Source</h2>
          <button @click="closeModal" class="close-btn">&times;</button>
        </div>
        <div class="modal-body">
          <form @submit.prevent="saveDataSource">
            <div class="form-group">
              <label>Name</label>
              <input
                v-model="formData.name"
                type="text"
                required
                class="form-control"
              />
            </div>
            <div class="form-group">
              <label>Type</label>
              <select v-model="formData.type" required class="form-control">
                <option value="API">API</option>
                <option value="Database">Database</option>
                <option value="File">File</option>
                <option value="Stream">Stream</option>
              </select>
            </div>
            <div class="form-group">
              <label>Connection String</label>
              <input
                v-model="formData.connectionString"
                type="text"
                required
                class="form-control"
                placeholder="e.g., https://api.example.com or Server=localhost;Database=mydb"
              />
            </div>
            <div class="form-group">
              <label>
                <input v-model="formData.isActive" type="checkbox" />
                Active
              </label>
            </div>
            <div class="form-group">
              <label>Configuration (JSON)</label>
              <textarea
                v-model="configurationJson"
                class="form-control"
                rows="4"
                placeholder='{"key": "value"}'
              ></textarea>
            </div>
            <div class="form-actions">
              <button
                type="button"
                @click="closeModal"
                class="btn btn-secondary"
              >
                Cancel
              </button>
              <button type="submit" class="btn btn-primary">
                {{ showEditModal ? "Update" : "Create" }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div
      v-if="showDeleteModal"
      class="modal-overlay"
      @click.self="showDeleteModal = false"
    >
      <div class="modal modal-sm">
        <div class="modal-header">
          <h2>Confirm Delete</h2>
        </div>
        <div class="modal-body">
          <p>Are you sure you want to delete "{{ selectedSource?.name }}"?</p>
        </div>
        <div class="modal-footer">
          <button @click="showDeleteModal = false" class="btn btn-secondary">
            Cancel
          </button>
          <button @click="confirmDelete" class="btn btn-danger">Delete</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import api from "../services/api";
import type { DataSource, QueryParams } from "../types";

// Reactive data
const dataSources = ref<DataSource[]>([]);
const loading = ref(false);
const searchTerm = ref("");
const filterType = ref("");
const filterStatus = ref("");
const currentPage = ref(1);
const pageSize = ref(10);
const totalCount = ref(0);

// Modal states
const showAddModal = ref(false);
const showEditModal = ref(false);
const showDeleteModal = ref(false);
const selectedSource = ref<DataSource | null>(null);

// Form data
const formData = ref<Partial<DataSource>>({
  name: "",
  type: "API",
  connectionString: "",
  isActive: true,
  configuration: {},
});
const configurationJson = ref("{}");

// Computed
const totalPages = computed(() => Math.ceil(totalCount.value / pageSize.value));

// Methods
const loadDataSources = async () => {
  loading.value = true;
  try {
    const params: QueryParams = {
      pageNumber: currentPage.value,
      pageSize: pageSize.value,
      searchTerm: searchTerm.value,
    };

    if (filterType.value) {
      params.type = filterType.value;
    }
    if (filterStatus.value) {
      params.isActive = filterStatus.value === "true";
    }

    const response = await api.dataSources.getAll(params);
    dataSources.value = response.items;
    totalCount.value = response.totalCount;
  } catch (error) {
    console.error("Failed to load data sources:", error);
  } finally {
    loading.value = false;
  }
};

const handleSearch = () => {
  currentPage.value = 1;
  loadDataSources();
};

const changePage = (page: number) => {
  currentPage.value = page;
  loadDataSources();
};

const formatDate = (dateString?: string) => {
  if (!dateString) return "Never";
  return new Date(dateString).toLocaleString();
};

const testConnection = async (source: DataSource) => {
  try {
    const result = await api.dataSources.testConnection(source.id);
    if (result.success) {
      alert("Connection successful!");
    } else {
      alert("Connection failed: " + result.message);
    }
  } catch (error) {
    console.error("Failed to test connection:", error);
    alert("Failed to test connection");
  }
};

const syncDataSource = async (source: DataSource) => {
  try {
    await api.dataSources.sync(source.id);
    alert("Sync initiated successfully!");
    loadDataSources();
  } catch (error) {
    console.error("Failed to sync data source:", error);
    alert("Failed to sync data source");
  }
};

const editDataSource = (source: DataSource) => {
  selectedSource.value = source;
  formData.value = { ...source };
  configurationJson.value = JSON.stringify(source.configuration || {}, null, 2);
  showEditModal.value = true;
};

const deleteDataSource = (source: DataSource) => {
  selectedSource.value = source;
  showDeleteModal.value = true;
};

const confirmDelete = async () => {
  if (!selectedSource.value) return;

  try {
    await api.dataSources.delete(selectedSource.value.id);
    showDeleteModal.value = false;
    selectedSource.value = null;
    loadDataSources();
  } catch (error) {
    console.error("Failed to delete data source:", error);
    alert("Failed to delete data source");
  }
};

const saveDataSource = async () => {
  try {
    // Parse configuration JSON
    try {
      formData.value.configuration = JSON.parse(configurationJson.value);
    } catch {
      alert("Invalid JSON configuration");
      return;
    }

    if (showEditModal.value && selectedSource.value) {
      await api.dataSources.update(selectedSource.value.id, formData.value);
    } else {
      await api.dataSources.create(formData.value);
    }

    closeModal();
    loadDataSources();
  } catch (error) {
    console.error("Failed to save data source:", error);
    alert("Failed to save data source");
  }
};

const closeModal = () => {
  showAddModal.value = false;
  showEditModal.value = false;
  selectedSource.value = null;
  formData.value = {
    name: "",
    type: "API",
    connectionString: "",
    isActive: true,
    configuration: {},
  };
  configurationJson.value = "{}";
};

// Lifecycle
onMounted(() => {
  loadDataSources();
});
</script>

<style scoped>
.data-sources {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.filters {
  display: flex;
  gap: 10px;
  margin-bottom: 20px;
}

.search-input {
  flex: 1;
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.filter-select {
  padding: 8px 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  background: white;
}

.table-container {
  background: white;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
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

.data-table td {
  padding: 12px;
  border-bottom: 1px solid #f0f0f0;
}

.data-table tr:hover {
  background: #f9f9f9;
}

.badge {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
}

.badge-api {
  background: #e3f2fd;
  color: #1976d2;
}

.badge-database {
  background: #f3e5f5;
  color: #7b1fa2;
}

.badge-file {
  background: #fff3e0;
  color: #f57c00;
}

.badge-stream {
  background: #e8f5e9;
  color: #388e3c;
}

.status {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
}

.status.active {
  background: #e8f5e9;
  color: #2e7d32;
}

.status:not(.active) {
  background: #ffebee;
  color: #c62828;
}

.actions {
  display: flex;
  gap: 5px;
}

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

.btn-primary:hover {
  background: #359268;
}

.btn-secondary {
  background: #6c757d;
  color: white;
}

.btn-secondary:hover {
  background: #5a6268;
}

.btn-danger {
  background: #dc3545;
  color: white;
}

.btn-danger:hover {
  background: #c82333;
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 10px;
  margin-top: 20px;
}

.page-info {
  padding: 0 10px;
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

.modal-sm {
  max-width: 400px;
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

.modal-footer {
  padding: 20px;
  border-top: 1px solid #e0e0e0;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}

.form-group {
  margin-bottom: 15px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
}

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

textarea.form-control {
  resize: vertical;
  font-family: monospace;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}

.text-center {
  text-align: center;
}
</style>
