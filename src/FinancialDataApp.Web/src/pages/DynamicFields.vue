<template>
  <div class="dynamic-fields">
    <div class="page-header">
      <h1>Dynamic Fields Management</h1>
      <button class="btn btn-primary" @click="showAddModal = true">
        Add Field
      </button>
    </div>

    <!-- Entity Type Filter -->
    <div class="filter-section">
      <label>Entity Type:</label>
      <select
        v-model="selectedEntityType"
        @change="loadFields"
        class="form-control"
      >
        <option value="">All Entities</option>
        <option value="RealTimeData">Real-Time Data</option>
        <option value="DataSource">Data Source</option>
        <option value="User">User</option>
      </select>
    </div>

    <!-- Fields Table -->
    <div class="table-container">
      <table class="fields-table">
        <thead>
          <tr>
            <th>Field Name</th>
            <th>Type</th>
            <th>Entity</th>
            <th>Required</th>
            <th>Default Value</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="7" class="text-center">Loading...</td>
          </tr>
          <tr v-else-if="fields.length === 0">
            <td colspan="7" class="text-center">No dynamic fields found</td>
          </tr>
          <tr v-for="field in fields" :key="field.id">
            <td>{{ field.fieldName }}</td>
            <td>
              <span class="type-badge">{{ field.fieldType }}</span>
            </td>
            <td>{{ field.entityType }}</td>
            <td>
              <span
                class="badge"
                :class="field.isRequired ? 'required' : 'optional'"
              >
                {{ field.isRequired ? "Required" : "Optional" }}
              </span>
            </td>
            <td>{{ field.defaultValue || "-" }}</td>
            <td>
              <span class="status" :class="{ active: field.isActive }">
                {{ field.isActive ? "Active" : "Inactive" }}
              </span>
            </td>
            <td class="actions">
              <button @click="editField(field)" class="btn btn-sm">Edit</button>
              <button @click="toggleActive(field)" class="btn btn-sm">
                {{ field.isActive ? "Deactivate" : "Activate" }}
              </button>
              <button @click="deleteField(field)" class="btn btn-sm btn-danger">
                Delete
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Add/Edit Modal -->
    <div
      v-if="showAddModal || showEditModal"
      class="modal-overlay"
      @click.self="closeModal"
    >
      <div class="modal">
        <div class="modal-header">
          <h2>{{ showEditModal ? "Edit" : "Add" }} Dynamic Field</h2>
          <button @click="closeModal" class="close-btn">&times;</button>
        </div>
        <div class="modal-body">
          <form @submit.prevent="saveField">
            <div class="form-group">
              <label>Field Name</label>
              <input
                v-model="formData.fieldName"
                type="text"
                required
                class="form-control"
              />
            </div>
            <div class="form-group">
              <label>Field Type</label>
              <select
                v-model="formData.fieldType"
                required
                class="form-control"
              >
                <option value="String">String</option>
                <option value="Number">Number</option>
                <option value="Boolean">Boolean</option>
                <option value="Date">Date</option>
                <option value="Json">JSON</option>
              </select>
            </div>
            <div class="form-group">
              <label>Entity Type</label>
              <select
                v-model="formData.entityType"
                required
                class="form-control"
              >
                <option value="RealTimeData">Real-Time Data</option>
                <option value="DataSource">Data Source</option>
                <option value="User">User</option>
              </select>
            </div>
            <div class="form-group">
              <label>
                <input v-model="formData.isRequired" type="checkbox" />
                Required Field
              </label>
            </div>
            <div class="form-group">
              <label>Default Value</label>
              <input
                v-model="formData.defaultValue"
                type="text"
                class="form-control"
              />
            </div>
            <div class="form-group">
              <label>Validation Rules (Regex)</label>
              <input
                v-model="formData.validationRules"
                type="text"
                class="form-control"
                placeholder="e.g., ^[A-Za-z0-9]+$"
              />
            </div>
            <div class="form-group">
              <label>Description</label>
              <textarea
                v-model="formData.description"
                class="form-control"
                rows="3"
              ></textarea>
            </div>
            <div class="form-group">
              <label>
                <input v-model="formData.isActive" type="checkbox" />
                Active
              </label>
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
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import api from "../services/api";
import type { DynamicField } from "../types";

// Reactive data
const fields = ref<DynamicField[]>([]);
const loading = ref(false);
const selectedEntityType = ref("");
const showAddModal = ref(false);
const showEditModal = ref(false);
const selectedField = ref<DynamicField | null>(null);

const formData = ref<Partial<DynamicField>>({
  fieldName: "",
  fieldType: "String",
  entityType: "RealTimeData",
  isRequired: false,
  defaultValue: "",
  validationRules: "",
  description: "",
  isActive: true,
});

// Methods
const loadFields = async () => {
  loading.value = true;
  try {
    if (selectedEntityType.value) {
      const data = await api.dynamicFields.getByEntity(
        selectedEntityType.value
      );
      fields.value = data;
    } else {
      const response = await api.dynamicFields.getAll({ pageSize: 100 });
      fields.value = response.items;
    }
  } catch (error) {
    console.error("Failed to load fields:", error);
  } finally {
    loading.value = false;
  }
};

const editField = (field: DynamicField) => {
  selectedField.value = field;
  formData.value = { ...field };
  showEditModal.value = true;
};

const toggleActive = async (field: DynamicField) => {
  try {
    await api.dynamicFields.update(field.id, { isActive: !field.isActive });
    loadFields();
  } catch (error) {
    console.error("Failed to toggle field status:", error);
  }
};

const deleteField = async (field: DynamicField) => {
  if (!confirm(`Delete field "${field.fieldName}"?`)) return;

  try {
    await api.dynamicFields.delete(field.id);
    loadFields();
  } catch (error) {
    console.error("Failed to delete field:", error);
  }
};

const saveField = async () => {
  try {
    if (showEditModal.value && selectedField.value) {
      await api.dynamicFields.update(selectedField.value.id, formData.value);
    } else {
      await api.dynamicFields.create(formData.value);
    }
    closeModal();
    loadFields();
  } catch (error) {
    console.error("Failed to save field:", error);
    alert("Failed to save field");
  }
};

const closeModal = () => {
  showAddModal.value = false;
  showEditModal.value = false;
  selectedField.value = null;
  formData.value = {
    fieldName: "",
    fieldType: "String",
    entityType: "RealTimeData",
    isRequired: false,
    defaultValue: "",
    validationRules: "",
    description: "",
    isActive: true,
  };
};

onMounted(() => {
  loadFields();
});
</script>

<style scoped>
.dynamic-fields {
  padding: 20px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.filter-section {
  background: white;
  padding: 20px;
  border-radius: 8px;
  margin-bottom: 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  display: flex;
  align-items: center;
  gap: 10px;
}

.filter-section label {
  font-weight: 500;
}

.filter-section .form-control {
  width: 200px;
}

.table-container {
  background: white;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.fields-table {
  width: 100%;
  border-collapse: collapse;
}

.fields-table th {
  background: #f5f5f5;
  padding: 12px;
  text-align: left;
  font-weight: 600;
  border-bottom: 2px solid #e0e0e0;
}

.fields-table td {
  padding: 12px;
  border-bottom: 1px solid #f0f0f0;
}

.type-badge {
  display: inline-block;
  padding: 4px 8px;
  background: #e3f2fd;
  color: #1976d2;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
}

.badge {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
}

.badge.required {
  background: #ffebee;
  color: #c62828;
}

.badge.optional {
  background: #e8f5e9;
  color: #2e7d32;
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

/* Modal and form styles */
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

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}

/* Button styles */
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

.text-center {
  text-align: center;
}
</style>
