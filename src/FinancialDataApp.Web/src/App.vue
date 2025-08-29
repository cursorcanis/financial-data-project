<template>
  <div id="app">
    <nav class="navbar">
      <div class="nav-brand">
        <router-link to="/">Financial Data App</router-link>
      </div>
      <ul class="nav-menu">
        <li>
          <router-link to="/" :class="{ active: $route.path === '/' }">
            Dashboard
          </router-link>
        </li>
        <li>
          <router-link
            to="/data-sources"
            :class="{ active: $route.path === '/data-sources' }"
          >
            Data Sources
          </router-link>
        </li>
        <li>
          <router-link
            to="/data-view"
            :class="{ active: $route.path === '/data-view' }"
          >
            Data View
          </router-link>
        </li>
        <li>
          <router-link
            to="/health"
            :class="{ active: $route.path === '/health' }"
          >
            System Health
          </router-link>
        </li>
        <li>
          <router-link
            to="/dynamic-fields"
            :class="{ active: $route.path === '/dynamic-fields' }"
          >
            Dynamic Fields
          </router-link>
        </li>
      </ul>
    </nav>
    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from "vue";
import signalRService from "./services/hubs";

// Initialize SignalR connection on app mount
onMounted(async () => {
  try {
    await signalRService.connect();
    console.log("SignalR connected");
  } catch (error) {
    console.error("Failed to connect SignalR:", error);
  }
});
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

#app {
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen,
    Ubuntu, Cantarell, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  min-height: 100vh;
  background: #f5f5f5;
}

.navbar {
  background: white;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  padding: 0 20px;
  display: flex;
  align-items: center;
  height: 60px;
  position: sticky;
  top: 0;
  z-index: 100;
}

.nav-brand {
  font-size: 20px;
  font-weight: 600;
  margin-right: 40px;
}

.nav-brand a {
  color: #42b883;
  text-decoration: none;
}

.nav-menu {
  display: flex;
  list-style: none;
  gap: 20px;
}

.nav-menu a {
  color: #333;
  text-decoration: none;
  padding: 8px 12px;
  border-radius: 4px;
  transition: all 0.2s;
}

.nav-menu a:hover {
  background: #f0f0f0;
}

.nav-menu a.active {
  background: #42b883;
  color: white;
}

.main-content {
  max-width: 1400px;
  margin: 0 auto;
  padding: 20px;
}

/* Responsive design */
@media (max-width: 768px) {
  .navbar {
    flex-direction: column;
    height: auto;
    padding: 10px;
  }

  .nav-brand {
    margin-right: 0;
    margin-bottom: 10px;
  }

  .nav-menu {
    flex-wrap: wrap;
    justify-content: center;
  }

  .main-content {
    padding: 10px;
  }
}
</style>
