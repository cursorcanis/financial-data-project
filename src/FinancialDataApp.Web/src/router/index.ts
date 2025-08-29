import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';
import Dashboard from '../pages/Dashboard.vue';
import DataSources from '../pages/DataSources.vue';
import DataView from '../pages/DataView.vue';
import HealthView from '../pages/HealthView.vue';
import DynamicFields from '../pages/DynamicFields.vue';

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'Dashboard',
    component: Dashboard,
    meta: { title: 'Dashboard' }
  },
  {
    path: '/data-sources',
    name: 'DataSources',
    component: DataSources,
    meta: { title: 'Data Sources' }
  },
  {
    path: '/data-view',
    name: 'DataView',
    component: DataView,
    meta: { title: 'Data View' }
  },
  {
    path: '/health',
    name: 'HealthView',
    component: HealthView,
    meta: { title: 'System Health' }
  },
  {
    path: '/dynamic-fields',
    name: 'DynamicFields',
    component: DynamicFields,
    meta: { title: 'Dynamic Fields' }
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

// Set page title based on route meta
router.beforeEach((to, _from, next) => {
  document.title = `${to.meta.title || 'Financial Data App'} - Financial Data App`;
  next();
});

export default router;
