<template>
  <div class="space-y-4">
    <div class="flex items-end justify-between flex-wrap gap-2">
      <div>
        <h2 class="text-xl font-bold">儀表板</h2>
        <p class="text-sm text-base-content/60">{{ year }} 年 {{ month }} 月營運總覽</p>
      </div>
      <div class="join">
        <button class="btn btn-sm join-item" @click="shiftMonth(-1)">‹ 上月</button>
        <button class="btn btn-sm join-item" @click="resetMonth">本月</button>
        <button class="btn btn-sm join-item" @click="shiftMonth(1)">下月 ›</button>
      </div>
    </div>

    <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-6 gap-4">
      <StatCard title="本月收入" :value="formatMoney(report.income)" />
      <StatCard title="本月支出" :value="formatMoney(report.expense)" />
      <StatCard
        title="本月損益"
        :value="formatMoney(report.profit)"
        :value-class="report.profit >= 0 ? 'text-success' : 'text-error'"
      />
      <StatCard title="未收款" :value="formatMoney(unpaidTotal)" :desc="`${unpaidCharges.length} 筆待收`" value-class="text-warning" />
      <StatCard title="即將到期合約" :value="expiringContracts.length" desc="60 天內到期" value-class="text-info" />
      <StatCard title="待處理報修" :value="pendingRepairs" desc="已送出 + 處理中" value-class="text-error" />
    </div>

    <div class="grid grid-cols-1 xl:grid-cols-2 gap-4">
      <div class="card bg-base-100 shadow">
        <div class="card-body p-4">
          <div class="flex items-center justify-between">
            <h3 class="card-title text-base">待收款項</h3>
            <RouterLink to="/admin/charges" class="btn btn-ghost btn-xs">查看全部 ›</RouterLink>
          </div>
          <p v-if="unpaidCharges.length === 0" class="text-sm text-base-content/60 py-4 text-center">目前沒有待收款項 🎉</p>
          <table v-else class="table table-sm">
            <thead>
              <tr>
                <th>合約</th>
                <th>類別</th>
                <th>帳期</th>
                <th class="text-right">金額</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in unpaidCharges.slice(0, 8)" :key="c.id">
                <td class="max-w-40 truncate">{{ c.contractName || c.contractNo }}</td>
                <td>{{ chargeCategoryName(c.category) }}</td>
                <td class="whitespace-nowrap">{{ formatDate(c.billingStartUtc) }} ~ {{ formatDate(c.billingEndUtc) }}</td>
                <td class="text-right">{{ formatMoney(c.amount) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="card bg-base-100 shadow">
        <div class="card-body p-4">
          <div class="flex items-center justify-between">
            <h3 class="card-title text-base">即將到期合約</h3>
            <RouterLink to="/admin/contracts" class="btn btn-ghost btn-xs">查看全部 ›</RouterLink>
          </div>
          <p v-if="expiringContracts.length === 0" class="text-sm text-base-content/60 py-4 text-center">60 天內沒有到期合約</p>
          <table v-else class="table table-sm">
            <thead>
              <tr>
                <th>合約</th>
                <th>租客</th>
                <th>到期日</th>
                <th class="text-right">剩餘天數</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in expiringContracts.slice(0, 8)" :key="c.id">
                <td class="max-w-40 truncate">{{ c.contractName || c.contractNo }}</td>
                <td>{{ c.tenant?.name || '-' }}</td>
                <td>{{ formatDate(c.endDateUtc) }}</td>
                <td class="text-right">
                  <span class="badge badge-sm" :class="daysLeft(c.endDateUtc) <= 30 ? 'badge-warning' : 'badge-ghost'">
                    {{ daysLeft(c.endDateUtc) }} 天
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <p v-if="error" class="text-error text-sm">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'
import StatCard from '../components/StatCard.vue'

type UnpaidCharge = {
  id: number
  contractName?: string | null
  contractNo?: string | null
  category: number
  billingStartUtc: string
  billingEndUtc: string
  amount: number
}

type ContractRow = {
  id: number
  contractNo: string
  contractName: string
  tenant?: { name?: string } | null
  endDateUtc: string
}

const now = new Date()
const year = ref(now.getFullYear())
const month = ref(now.getMonth() + 1)
const report = ref({ income: 0, expense: 0, profit: 0 })
const unpaidCharges = ref<UnpaidCharge[]>([])
const unpaidTotal = ref(0)
const expiringContracts = ref<ContractRow[]>([])
const pendingRepairs = ref(0)
const error = ref('')

const chargeCategoryNames: Record<number, string> = { 1: '租金', 2: '水費', 3: '電費', 99: '其他' }
const chargeCategoryName = (value: number) => chargeCategoryNames[value] || '其他'

const formatMoney = (value: number) => `$${Number(value || 0).toLocaleString()}`
const formatDate = (value?: string) => (value ? value.slice(0, 10) : '')
const daysLeft = (endDateUtc: string) => {
  const diff = new Date(endDateUtc).getTime() - Date.now()
  return Math.max(0, Math.ceil(diff / 86400000))
}

const loadReport = async () => {
  const { data } = await api.get(`/reports/monthly?year=${year.value}&month=${month.value}`)
  report.value = data
}

const loadUnpaid = async () => {
  const { data } = await api.get('/charges', { params: { isPaid: false } })
  unpaidCharges.value = data
  unpaidTotal.value = data.reduce((sum: number, x: UnpaidCharge) => sum + Number(x.amount || 0), 0)
}

const loadExpiring = async () => {
  const { data } = await api.get('/contracts', { params: { status: 1 } })
  const limit = Date.now() + 60 * 86400000
  expiringContracts.value = data
    .filter((c: ContractRow) => new Date(c.endDateUtc).getTime() <= limit)
    .sort((a: ContractRow, b: ContractRow) => a.endDateUtc.localeCompare(b.endDateUtc))
}

const loadRepairs = async () => {
  const { data } = await api.get('/repair-tickets')
  pendingRepairs.value = data.filter((t: any) => t.status === 1 || t.status === 2).length
}

const load = async () => {
  error.value = ''
  try {
    await Promise.all([loadReport(), loadUnpaid(), loadExpiring(), loadRepairs()])
  } catch (e: any) {
    error.value = e?.response?.data || e?.message || '載入儀表板資料失敗'
  }
}

const shiftMonth = async (delta: number) => {
  const d = new Date(year.value, month.value - 1 + delta, 1)
  year.value = d.getFullYear()
  month.value = d.getMonth() + 1
  await loadReport().catch(() => {})
}

const resetMonth = async () => {
  year.value = now.getFullYear()
  month.value = now.getMonth() + 1
  await loadReport().catch(() => {})
}

onMounted(load)
</script>
