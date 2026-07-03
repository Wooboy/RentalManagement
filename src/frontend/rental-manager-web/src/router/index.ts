import type { RouteRecordRaw, Router } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import AdminLayout from '../layouts/AdminLayout.vue'
import PortalLayout from '../layouts/PortalLayout.vue'
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
import PortalHomeView from '../views/portal/PortalHomeView.vue'

export const routes: RouteRecordRaw[] = [
  { path: '/login', component: LoginView },
  {
    path: '/admin',
    component: AdminLayout,
    meta: { requiresRole: 1 },
    children: [
      { path: '', component: DashboardView },
      { path: 'tenants', component: TenantsView },
      { path: 'properties', component: PropertiesView },
      { path: 'contracts', component: ContractsView },
      { path: 'charges', component: ChargesView },
      { path: 'expenses', component: ExpensesView },
      { path: 'electricity', component: ElectricityView },
      { path: 'electricity-meter-readings', component: ElectricityMeterReadingsView },
      { path: 'users', component: AdminUsersView }
    ]
  },
  {
    path: '/portal',
    component: PortalLayout,
    meta: { requiresRole: 2 },
    children: [
      { path: '', component: PortalHomeView }
    ]
  },
  // 舊路徑轉址（保留書籤相容）
  { path: '/', redirect: '/admin' },
  { path: '/tenants', redirect: '/admin/tenants' },
  { path: '/properties', redirect: '/admin/properties' },
  { path: '/contracts', redirect: '/admin/contracts' },
  { path: '/charges', redirect: '/admin/charges' },
  { path: '/expenses', redirect: '/admin/expenses' },
  { path: '/electricity', redirect: '/admin/electricity' },
  { path: '/electricity-meter-readings', redirect: '/admin/electricity-meter-readings' },
  { path: '/admin-users', redirect: '/admin/users' },
  { path: '/:pathMatch(.*)*', redirect: '/admin' }
]

export const applyAuthGuard = (router: Router) => {
  router.beforeEach((to) => {
    const auth = useAuthStore()
    const isLoginPage = to.path === '/login'

    if (!auth.isLoggedIn && !isLoginPage) {
      return { path: '/login', query: { redirect: to.fullPath } }
    }

    if (auth.isLoggedIn && isLoginPage) {
      return typeof to.query.redirect === 'string' ? to.query.redirect : auth.homePath
    }

    const requiresRole = to.matched.find((r) => r.meta.requiresRole)?.meta.requiresRole
    if (requiresRole && auth.role !== requiresRole) {
      return auth.homePath
    }

    return true
  })
}
