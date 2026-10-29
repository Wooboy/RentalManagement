<template>
  <div class="space-y-4">
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <StatCard title="生效中合約" :value="activeContracts" />
      <StatCard title="未繳金額" :value="`$${unpaidTotal.toLocaleString()}`" :desc="`${unpaidCount} 筆未繳`" value-class="text-warning" />
      <StatCard title="處理中報修" :value="openRepairs" value-class="text-info" />
    </div>

    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <RouterLink to="/portal/contracts" class="card bg-base-100 shadow hover:shadow-md transition-shadow">
        <div class="card-body p-5">
          <h3 class="card-title text-base">我的合約</h3>
          <p class="text-sm text-base-content/60">查看合約內容、租期與附件</p>
        </div>
      </RouterLink>
      <RouterLink to="/portal/charges" class="card bg-base-100 shadow hover:shadow-md transition-shadow">
        <div class="card-body p-5">
          <h3 class="card-title text-base">我的帳單</h3>
          <p class="text-sm text-base-content/60">查看租金與水電費繳費紀錄</p>
        </div>
      </RouterLink>
      <RouterLink to="/portal/repairs" class="card bg-base-100 shadow hover:shadow-md transition-shadow">
        <div class="card-body p-5">
          <h3 class="card-title text-base">線上報修</h3>
          <p class="text-sm text-base-content/60">回報修繕需求並追蹤進度</p>
        </div>
      </RouterLink>
    </div>

    <p v-if="error" class="text-error text-sm">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../../services/api'
import StatCard from '../../components/StatCard.vue'

const activeContracts = ref(0)
const unpaidTotal = ref(0)
const unpaidCount = ref(0)
const openRepairs = ref(0)
const error = ref('')

onMounted(async () => {
  try {
    const [contracts, unpaid, repairs] = await Promise.all([
      api.get('/portal/contracts'),
      api.get('/portal/charges', { params: { isPaid: false } }),
      api.get('/portal/repair-tickets')
    ])
    activeContracts.value = contracts.data.filter((c: any) => c.status === 1).length
    unpaidCount.value = unpaid.data.length
    unpaidTotal.value = unpaid.data.reduce((sum: number, x: any) => sum + Number(x.amount || 0), 0)
    openRepairs.value = repairs.data.filter((r: any) => r.status === 1 || r.status === 2).length
  } catch (e: any) {
    error.value = e?.response?.data || e?.message || '載入資料失敗'
  }
})
</script>
