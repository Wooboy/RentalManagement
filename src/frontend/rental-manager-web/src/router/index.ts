import type { RouteRecordRaw, Router } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import DashboardView from '../views/DashboardView.vue'
import TenantsView from '../views/TenantsView.vue'
import PropertiesView from '../views/PropertiesView.vue'
import ContractsView from '../views/ContractsView.vue'
import ChargesView from '../views/ChargesView.vue'
import ExpensesView from '../views/ExpensesView.vue'
import ElectricityView from '../views/ElectricityView.vue'
import ElectricityMeterReadingsView from '../views/ElectricityMeterReadingsView.vue'
import AdminUsersView from '../views/AdminUsersView.vue'

export const routes: RouteRecordRaw[] = [
  { path: '/', component: DashboardView },
  { path: '/login', component: LoginView },
  { path: '/tenants', component: TenantsView },
  { path: '/properties', component: PropertiesView },
  { path: '/contracts', component: ContractsView },
  { path: '/charges', component: ChargesView },
  { path: '/expenses', component: ExpensesView },
  { path: '/electricity', component: ElectricityView },
  { path: '/electricity-meter-readings', component: ElectricityMeterReadingsView },
  { path: '/admin-users', component: AdminUsersView, meta: { requiresAdmin: true } }
]

export const applyAuthGuard = (router: Router) => {
  router.beforeEach((to) => {
    const token = localStorage.getItem('token')
    const role = localStorage.getItem('auth_role')
    const isLoginPage = to.path === '/login'

    if (!token && !isLoginPage) {
      return { path: '/login', query: { redirect: to.fullPath } }
    }

    if (token && isLoginPage) {
      return typeof to.query.redirect === 'string' ? to.query.redirect : '/'
    }

    if (to.meta.requiresAdmin && role !== '1') {
      return '/'
    }

    return true
  })
}
