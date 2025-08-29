<template>
  <div class="health-view">
    <h1>System Health</h1>

    <!-- Health Status Summary -->
    <div class="health-summary">
      <div class="status-card" :class="overallStatus">
        <h2>Overall Status</h2>
        <span class="status-text">{{ overallStatus }}</span>
      </div>
      <div class="stats-row">
        <div class="stat-item">
          <label>Healthy Components</label>
          <span class="stat-value">{{ healthyCount }}</span>
        </div>
        <div class="stat-item">
          <label>Degraded Components</label>
          <span class="stat-value warning">{{ degradedCount }}</span>
        </div>
        <div class="stat-item">
          <label>Unhealthy Components</label>
          <span class="stat-value error">{{ unhealthyCount }}</span>
        </div>
      </div>
    </div>

    <!-- Components Grid -->
    <div class="components-grid">
      <div
        v-for="component in healthData"
        :key="component.component"
        class="component-card"
        :class="component.status.toLowerCase()"
      >
        <div class="component-header">
          <h3>{{ component.component }}</h3>
          <span class="status-badge" :class="component.status.toLowerCase()">
            {{ component.status }}
          </span>
        </div>
        <div class="metrics">
          <div class="metric">
            <label>CPU Usage</label>
            <div class="progress-bar">
              <div
                class="progress-fill"
                :style="{ width: component.cpuUsage + '%' }"
                :class="getMetricClass(component.cpuUsage)"
              ></div>
            </div>
            <span>{{ component.cpuUsage }}%</span>
          </div>
          <div class="metric">
            <label>Memory Usage</label>
            <div class="progress-bar">
              <div
                class="progress-fill"
                :style="{ width: component.memoryUsage + '%' }"
                :class="getMetricClass(component.memoryUsage)"
              ></div>
            </div>
            <span>{{ component.memoryUsage }}%</span>
          </div>
          <div class="metric">
            <label>Disk Usage</label>
            <div class="progress-bar">
              <div
                class="progress-fill"
                :style="{ width: component.diskUsage + '%' }"
                :class="getMetricClass(component.diskUsage)"
              ></div>
            </div>
            <span>{{ component.diskUsage }}%</span>
          </div>
          <div class="metric">
            <label>Response Time</label>
            <span class="metric-value">{{ component.responseTime }}ms</span>
          </div>
          <div class="metric">
            <label>Error Rate</label>
            <span
              class="metric-value"
              :class="{ error: component.errorRate > 5 }"
            >
              {{ component.errorRate }}%
            </span>
          </div>
        </div>
        <div class="component-footer">
          <span class="timestamp"
            >Last Updated: {{ formatTimestamp(component.timestamp) }}</span
          >
        </div>
      </div>
    </div>

    <!-- Actions -->
    <div class="actions-section">
      <button
        @click="runHealthCheck"
        class="btn btn-primary"
        :disabled="loading"
      >
        {{ loading ? "Running..." : "Run Health Check" }}
      </button>
      <button @click="refreshData" class="btn btn-secondary">Refresh</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from "vue";
import api from "../services/api";
import signalRService from "../services/hubs";
import type { SystemHealth } from "../types";

// Reactive data
const healthData = ref<SystemHealth[]>([]);
const loading = ref(false);
const refreshInterval = ref<number | null>(null);

// Computed properties
const overallStatus = computed(() => {
  if (healthData.value.length === 0) return "unknown";
  if (healthData.value.some((h) => h.status === "Unhealthy"))
    return "unhealthy";
  if (healthData.value.some((h) => h.status === "Degraded")) return "degraded";
  return "healthy";
});

const healthyCount = computed(
  () => healthData.value.filter((h) => h.status === "Healthy").length
);

const degradedCount = computed(
  () => healthData.value.filter((h) => h.status === "Degraded").length
);

const unhealthyCount = computed(
  () => healthData.value.filter((h) => h.status === "Unhealthy").length
);

// Methods
const loadHealthData = async () => {
  try {
    const data = await api.health.getCurrent();
    healthData.value = data;
  } catch (error) {
    console.error("Failed to load health data:", error);
  }
};

const runHealthCheck = async () => {
  loading.value = true;
  try {
    const result = await api.health.runHealthCheck();
    if (result.success) {
      healthData.value = result.data;
      alert("Health check completed successfully");
    } else {
      alert("Health check failed: " + result.message);
    }
  } catch (error) {
    console.error("Failed to run health check:", error);
    alert("Failed to run health check");
  } finally {
    loading.value = false;
  }
};

const refreshData = () => {
  loadHealthData();
};

const formatTimestamp = (timestamp: string) => {
  return new Date(timestamp).toLocaleString();
};

const getMetricClass = (value: number) => {
  if (value >= 90) return "critical";
  if (value >= 75) return "warning";
  return "normal";
};

// Lifecycle
onMounted(() => {
  loadHealthData();

  // Set up auto-refresh every 30 seconds
  refreshInterval.value = window.setInterval(() => {
    loadHealthData();
  }, 30000);

  // Set up SignalR handler for real-time updates
  signalRService.setHealthUpdateHandler((update) => {
    const index = healthData.value.findIndex(
      (h) => h.component === update.component
    );
    if (index >= 0) {
      healthData.value[index] = {
        ...healthData.value[index],
        status: update.status,
        cpuUsage: update.metrics.cpuUsage,
        memoryUsage: update.metrics.memoryUsage,
        responseTime: update.metrics.responseTime,
        timestamp: new Date().toISOString(),
      };
    }
  });
});

onUnmounted(() => {
  if (refreshInterval.value) {
    clearInterval(refreshInterval.value);
  }
  signalRService.removeHealthUpdateHandler();
});
</script>

<style scoped>
.health-view {
  padding: 20px;
}

.health-summary {
  background: white;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 30px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.status-card {
  text-align: center;
  padding: 20px;
  border-radius: 8px;
  margin-bottom: 20px;
}

.status-card.healthy {
  background: #e8f5e9;
  color: #2e7d32;
}

.status-card.degraded {
  background: #fff3e0;
  color: #f57c00;
}

.status-card.unhealthy {
  background: #ffebee;
  color: #c62828;
}

.status-card h2 {
  margin: 0 0 10px 0;
  font-size: 18px;
  text-transform: uppercase;
}

.status-text {
  font-size: 24px;
  font-weight: bold;
  text-transform: capitalize;
}

.stats-row {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}

.stat-item {
  text-align: center;
}

.stat-item label {
  display: block;
  font-size: 14px;
  color: #666;
  margin-bottom: 5px;
}

.stat-value {
  font-size: 28px;
  font-weight: bold;
  color: #333;
}

.stat-value.warning {
  color: #f57c00;
}

.stat-value.error {
  color: #c62828;
}

.components-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

.component-card {
  background: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  border-left: 4px solid #ccc;
}

.component-card.healthy {
  border-left-color: #4caf50;
}

.component-card.degraded {
  border-left-color: #ff9800;
}

.component-card.unhealthy {
  border-left-color: #f44336;
}

.component-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
}

.component-header h3 {
  margin: 0;
  font-size: 18px;
}

.status-badge {
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
}

.status-badge.healthy {
  background: #e8f5e9;
  color: #2e7d32;
}

.status-badge.degraded {
  background: #fff3e0;
  color: #f57c00;
}

.status-badge.unhealthy {
  background: #ffebee;
  color: #c62828;
}

.metrics {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.metric {
  display: flex;
  align-items: center;
  gap: 10px;
}

.metric label {
  flex: 0 0 120px;
  font-size: 14px;
  color: #666;
}

.progress-bar {
  flex: 1;
  height: 20px;
  background: #f0f0f0;
  border-radius: 10px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: #4caf50;
  transition: width 0.3s ease;
}

.progress-fill.warning {
  background: #ff9800;
}

.progress-fill.critical {
  background: #f44336;
}

.metric-value {
  font-weight: 600;
  color: #333;
}

.metric-value.error {
  color: #f44336;
}

.component-footer {
  margin-top: 15px;
  padding-top: 15px;
  border-top: 1px solid #e0e0e0;
}

.timestamp {
  font-size: 12px;
  color: #999;
}

.actions-section {
  display: flex;
  gap: 10px;
  justify-content: center;
}

.btn {
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  transition: background-color 0.2s;
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
</style>
