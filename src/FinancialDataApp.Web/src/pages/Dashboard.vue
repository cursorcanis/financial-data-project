<template>
  <div class="dashboard">
    <h1>Dashboard</h1>

    <!-- Connection Status -->
    <div class="connection-status" :class="{ connected: isConnected }">
      <span class="status-dot"></span>
      {{ isConnected ? "Connected" : "Disconnected" }}
    </div>

    <!-- Ingestion Stats -->
    <div class="stats-grid">
      <div class="stat-card">
        <h3>Ingestion Rate</h3>
        <div class="stat-value">{{ ingestionStats.rate }} rec/s</div>
      </div>
      <div class="stat-card">
        <h3>Total Records</h3>
        <div class="stat-value">
          {{ ingestionStats.totalRecords.toLocaleString() }}
        </div>
      </div>
      <div class="stat-card">
        <h3>Success Rate</h3>
        <div class="stat-value">{{ successRate }}%</div>
      </div>
      <div class="stat-card">
        <h3>Errors</h3>
        <div class="stat-value error">{{ ingestionStats.errorCount }}</div>
      </div>
    </div>

    <!-- Charts -->
    <div class="charts-grid">
      <div class="chart-container">
        <h3>Ingestion Rate Over Time</h3>
        <Line
          v-if="chartData.labels.length > 0"
          :data="chartData"
          :options="chartOptions"
        />
      </div>

      <div class="chart-container">
        <h3>Data Sources Activity</h3>
        <Bar
          v-if="dataSourceChartData.labels.length > 0"
          :data="dataSourceChartData"
          :options="barChartOptions"
        />
      </div>
    </div>

    <!-- Recent Activity -->
    <div class="recent-activity">
      <h3>Recent Activity</h3>
      <div class="activity-list">
        <div
          v-for="activity in recentActivities"
          :key="activity.id"
          class="activity-item"
        >
          <span class="activity-time">{{
            formatTime(activity.timestamp)
          }}</span>
          <span class="activity-source">{{ activity.source }}</span>
          <span class="activity-count">{{ activity.count }} records</span>
        </div>
      </div>
    </div>

    <!-- System Health -->
    <div class="system-health">
      <h3>System Health</h3>
      <div class="health-grid">
        <div
          v-for="health in systemHealth"
          :key="health.component"
          class="health-item"
          :class="health.status.toLowerCase()"
        >
          <span class="health-component">{{ health.component }}</span>
          <span class="health-status">{{ health.status }}</span>
          <div class="health-metrics">
            <span>CPU: {{ health.metrics.cpuUsage }}%</span>
            <span>Memory: {{ health.metrics.memoryUsage }}%</span>
            <span>Response: {{ health.metrics.responseTime }}ms</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from "vue";
import { Line, Bar } from "vue-chartjs";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from "chart.js";
import signalRService from "../services/hubs";
import api from "../services/api";
import type { IngestionUpdate, HealthUpdate, ChartData } from "../types";

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
  Filler
);

// Reactive data
const isConnected = ref(false);
const ingestionStats = ref<IngestionUpdate>({
  timestamp: new Date().toISOString(),
  rate: 0,
  totalRecords: 0,
  successCount: 0,
  errorCount: 0,
});

const chartData = ref<ChartData>({
  labels: [],
  datasets: [
    {
      label: "Ingestion Rate",
      data: [],
      borderColor: "rgb(75, 192, 192)",
      backgroundColor: "rgba(75, 192, 192, 0.2)",
      borderWidth: 2,
    },
  ],
});

const dataSourceChartData = ref<ChartData>({
  labels: [],
  datasets: [
    {
      label: "Records Processed",
      data: [],
      backgroundColor: "rgba(54, 162, 235, 0.5)",
      borderColor: "rgb(54, 162, 235)",
      borderWidth: 1,
    },
  ],
});

const recentActivities = ref<any[]>([]);
const systemHealth = ref<HealthUpdate[]>([]);

// Chart options
const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      display: true,
      position: "top" as const,
    },
    title: {
      display: false,
    },
  },
  scales: {
    x: {
      display: true,
      grid: {
        display: false,
      },
    },
    y: {
      display: true,
      beginAtZero: true,
    },
  },
};

const barChartOptions = {
  ...chartOptions,
  indexAxis: "x" as const,
};

// Computed properties
const successRate = computed(() => {
  const total =
    ingestionStats.value.successCount + ingestionStats.value.errorCount;
  if (total === 0) return 100;
  return Math.round((ingestionStats.value.successCount / total) * 100);
});

// Methods
const formatTime = (timestamp: string) => {
  const date = new Date(timestamp);
  return date.toLocaleTimeString();
};

const updateChartData = (update: IngestionUpdate) => {
  // Keep only last 20 data points
  const maxDataPoints = 20;

  const time = formatTime(update.timestamp);

  // Update line chart
  if (chartData.value.labels.length >= maxDataPoints) {
    chartData.value.labels.shift();
    chartData.value.datasets[0].data.shift();
  }

  chartData.value.labels.push(time);
  chartData.value.datasets[0].data.push(update.rate);

  // Update recent activities
  recentActivities.value.unshift({
    id: Date.now(),
    timestamp: update.timestamp,
    source: "System",
    count: update.rate,
  });

  if (recentActivities.value.length > 10) {
    recentActivities.value.pop();
  }
};

const loadInitialData = async () => {
  try {
    // Load data sources statistics
    const dataSources = await api.dataSources.getAll({ pageSize: 10 });
    if (dataSources.items.length > 0) {
      dataSourceChartData.value.labels = dataSources.items.map((ds) => ds.name);
      // This would need actual statistics from the API
      dataSourceChartData.value.datasets[0].data = dataSources.items.map(() =>
        Math.floor(Math.random() * 1000)
      );
    }

    // Load system health
    const health = await api.health.getCurrent();
    systemHealth.value = health.map((h) => ({
      component: h.component,
      status: h.status,
      metrics: {
        cpuUsage: h.cpuUsage,
        memoryUsage: h.memoryUsage,
        responseTime: h.responseTime,
      },
    }));
  } catch (error) {
    console.error("Failed to load initial data:", error);
  }
};

// Lifecycle hooks
onMounted(async () => {
  // Set up SignalR handlers
  signalRService.setConnectionStateHandler((connected) => {
    isConnected.value = connected;
  });

  signalRService.setIngestionUpdateHandler((update) => {
    ingestionStats.value = update;
    updateChartData(update);
  });

  signalRService.setHealthUpdateHandler((update) => {
    const index = systemHealth.value.findIndex(
      (h) => h.component === update.component
    );
    if (index >= 0) {
      systemHealth.value[index] = update;
    } else {
      systemHealth.value.push(update);
    }
  });

  // Connect to SignalR
  try {
    await signalRService.connect();
  } catch (error) {
    console.error("Failed to connect to SignalR:", error);
  }

  // Load initial data
  await loadInitialData();
});

onUnmounted(() => {
  // Clean up SignalR handlers
  signalRService.removeConnectionStateHandler();
  signalRService.removeIngestionUpdateHandler();
  signalRService.removeHealthUpdateHandler();
});
</script>

<style scoped>
.dashboard {
  padding: 20px;
}

.connection-status {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background: #f5f5f5;
  border-radius: 20px;
  margin-bottom: 20px;
}

.connection-status.connected {
  background: #e8f5e9;
  color: #2e7d32;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #f44336;
}

.connected .status-dot {
  background: #4caf50;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0% {
    box-shadow: 0 0 0 0 rgba(76, 175, 80, 0.7);
  }
  70% {
    box-shadow: 0 0 0 10px rgba(76, 175, 80, 0);
  }
  100% {
    box-shadow: 0 0 0 0 rgba(76, 175, 80, 0);
  }
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

.stat-card {
  background: white;
  padding: 20px;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.stat-card h3 {
  margin: 0 0 10px 0;
  font-size: 14px;
  color: #666;
  text-transform: uppercase;
}

.stat-value {
  font-size: 28px;
  font-weight: bold;
  color: #333;
}

.stat-value.error {
  color: #f44336;
}

.charts-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
  gap: 20px;
  margin-bottom: 30px;
}

.chart-container {
  background: white;
  padding: 20px;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  min-height: 300px;
}

.chart-container h3 {
  margin: 0 0 20px 0;
  font-size: 18px;
}

.recent-activity,
.system-health {
  background: white;
  padding: 20px;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  margin-bottom: 20px;
}

.recent-activity h3,
.system-health h3 {
  margin: 0 0 20px 0;
  font-size: 18px;
}

.activity-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.activity-item {
  display: grid;
  grid-template-columns: 120px 1fr auto;
  gap: 20px;
  padding: 10px;
  background: #f9f9f9;
  border-radius: 4px;
}

.activity-time {
  color: #666;
  font-size: 14px;
}

.activity-source {
  font-weight: 500;
}

.activity-count {
  color: #42b883;
  font-weight: bold;
}

.health-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 15px;
}

.health-item {
  padding: 15px;
  border-radius: 8px;
  background: #f5f5f5;
  border-left: 4px solid #ccc;
}

.health-item.healthy {
  border-left-color: #4caf50;
  background: #e8f5e9;
}

.health-item.degraded {
  border-left-color: #ff9800;
  background: #fff3e0;
}

.health-item.unhealthy {
  border-left-color: #f44336;
  background: #ffebee;
}

.health-component {
  font-weight: bold;
  display: block;
  margin-bottom: 5px;
}

.health-status {
  display: block;
  margin-bottom: 10px;
  font-size: 14px;
  text-transform: uppercase;
}

.health-metrics {
  display: flex;
  gap: 15px;
  font-size: 12px;
  color: #666;
}
</style>
