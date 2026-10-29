<template>
  <div class="space-y-6">
    <PageHeader title="儀表板" :subtitle="`${year} 年 ${month} 月營運總覽`">
      <template #actions>
        <div class="join">
          <button class="btn btn-sm join-item" @click="shiftMonth(-1)">‹ 上月</button>
          <button class="btn btn-sm join-item" @click="resetMonth">本月</button>
          <button class="btn btn-sm join-item" @click="shiftMonth(1)">下月 ›</button>
        </div>
      </template>
    </PageHeader>

    <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-6 gap-4">
      <StatCard title="本月收入" :value="formatMoney(report.income)" accent="success"
        icon="M12 6v12m-3-2.818.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-2.003-.659-1.106-.879-1.106-2.303 0-3.182s2.9-.879 4.006 0l.415.33M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
      <StatCard title="本月支出" :value="formatMoney(report.expense)" accent="neutral"
        icon="M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-3.75 3h15a2.25 2.25 0 0 0 2.25-2.25V6.75A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25v10.5A2.25 2.25 0 0 0 4.5 19.5Z" />
      <StatCard
        title="本月損益"
        :value="formatMoney(report.profit)"
        :value-class="report.profit >= 0 ? 'text-success' : 'text-error'"
        :accent="report.profit >= 0 ? 'success' : 'error'"
        icon="M2.25 18 9 11.25l4.306 4.306a11.95 11.95 0 0 1 5.814-5.518l2.74-1.22m0 0-5.94-2.281m5.94 2.28-2.28 5.941" />
      <StatCard title="未收款" :value="formatMoney(unpaidTotal)" :desc="`${unpaidCharges.length} 筆待收`" value-class="text-warning" accent="warning"
        icon="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" />
      <StatCard title="即將到期合約" :value="expiringContracts.length" desc="60 天內到期" value-class="text-info" accent="info"
        icon="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
      <StatCard title="待處理報修" :value="pendingRepairs" desc="已送出 + 處理中" value-class="text-error" accent="error"
        icon="M11.42 15.17 17.25 21A2.652 2.652 0 0 0 21 17.25l-5.877-5.877M11.42 15.17l2.496-3.03c.317-.384.74-.626 1.208-.766M11.42 15.17l-4.655 5.653a2.548 2.548 0 1 1-3.586-3.586l6.837-5.63m5.108-.233c.55-.164 1.163-.188 1.743-.14a4.5 4.5 0 0 0 4.486-6.336l-3.276 3.277a3.004 3.004 0 0 1-2.25-2.25l3.276-3.276a4.5 4.5 0 0 0-6.336 4.486c.091 1.076-.071 2.264-.904 2.95l-.102.085" />
    </div>

    <div class="grid grid-cols-1 xl:grid-cols-2 gap-4">
      <div class="card bg-base-100 border border-base-300 shadow-sm">
        <div class="card-body p-5">
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

      <div class="card bg-base-100 border border-base-300 shadow-sm">
        <div class="card-body p-5">
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
import PageHeader from '../components/PageHeader.vue'

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
  status: number
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
  const now = Date.now()
  const limit = now + 60 * 86400000
  // 只留下「仍生效（衍生狀態）且在 60 天內、尚未到期」的合約，排除已過期未手動更新者
  expiringContracts.value = data
    .filter((c: ContractRow) => c.status === 1)
    .filter((c: ContractRow) => {
      const end = new Date(c.endDateUtc).getTime()
      return end >= now && end <= limit
    })
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
