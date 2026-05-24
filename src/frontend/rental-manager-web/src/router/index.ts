import type { RouteRecordRaw } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import DashboardView from '../views/DashboardView.vue'
import TenantsView from '../views/TenantsView.vue'
import PropertiesView from '../views/PropertiesView.vue'
import ContractsView from '../views/ContractsView.vue'
import ChargesView from '../views/ChargesView.vue'
import ExpensesView from '../views/ExpensesView.vue'
import ElectricityView from '../views/ElectricityView.vue'

export const routes: RouteRecordRaw[] = [
  { path: '/', component: DashboardView },
  { path: '/login', component: LoginView },
  { path: '/tenants', component: TenantsView },
  { path: '/properties', component: PropertiesView },
  { path: '/contracts', component: ContractsView },
  { path: '/charges', component: ChargesView },
  { path: '/expenses', component: ExpensesView },
  { path: '/electricity', component: ElectricityView }
]
