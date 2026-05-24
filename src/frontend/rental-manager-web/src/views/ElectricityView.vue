<template>
  <div class="space-y-4">
    <div class="card bg-base-100 shadow p-4">
      <h2 class="text-lg font-bold mb-3">電費計算</h2>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-2 mb-3">
        <label class="form-control"><span class="label-text mb-1">1. 計算方式</span>
          <select v-model.number="calcMode" class="select select-bordered">
            <option :value="1">依度數計算</option>
            <option :value="2">平均計算</option>
            <option :value="3">多錶計算</option>
          </select>
        </label>

        <label class="form-control"><span class="label-text mb-1">2. 選擇電費帳單（可多選）</span>
          <select class="select select-bordered" @change="noop">
            <option>請在下方勾選帳單</option>
          </select>
        </label>

        <label class="form-control"><span class="label-text mb-1">關聯合約</span>
          <select v-model.number="form.contractId" class="select select-bordered">
            <option :value="0">請選擇合約</option>
            <option v-for="c in relatedContracts" :key="c.id" :value="c.id">{{ c.contractNo }} - {{ c.tenant?.name }}</option>
          </select>
        </label>
      </div>

      <div class="overflow-x-auto mb-3">
        <table class="table table-zebra">
          <thead><tr><th></th><th>帳單ID</th><th>帳期</th><th>金額</th><th>度數</th><th>房源/房間</th></tr></thead>
          <tbody>
            <tr v-for="b in electricityExpenseBills" :key="b.id">
              <td><input type="checkbox" class="checkbox checkbox-sm" :checked="selectedExpenseBillIds.includes(b.id)" @change="toggleBill(b)" /></td>
              <td>#{{ b.id }}</td>
              <td>{{ b.billingStartUtc?.slice(0,10) }} ~ {{ b.billingEndUtc?.slice(0,10) }}</td>
              <td>{{ b.amount }}</td>
              <td>{{ b.usageUnits ?? '-' }}</td>
              <td>{{ b.propertyUnitName || '-' }} / {{ b.propertyRoomName || '-' }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-4 gap-2 mb-3">
        <label class="form-control"><span class="label-text mb-1">帳期起日</span><input v-model="form.billingStart" type="date" max="2099-12-31" class="input input-bordered" /></label>
        <label class="form-control"><span class="label-text mb-1">帳期迄日</span><input v-model="form.billingEnd" type="date" max="2099-12-31" class="input input-bordered" /></label>
        <label class="form-control"><span class="label-text mb-1">帳單總額（加總）</span><input :value="aggregatedAmount" type="number" class="input input-bordered" disabled /></label>
        <label class="form-control"><span class="label-text mb-1">總度數（加總）</span><input :value="aggregatedUnits" type="number" class="input input-bordered" disabled /></label>
      </div>

      <div class="overflow-x-auto">
        <p class="text-sm mb-2">3. 關聯租客名單（自動帶入，可調整）</p>
        <table class="table table-zebra">
          <thead><tr><th>租客</th><th>租客度數</th><th>人數</th><th>入住天數</th><th></th></tr></thead>
          <tbody>
            <tr v-for="(a, idx) in form.allocations" :key="idx">
              <td>{{ a.tenantName || '-' }}</td>
              <td><input v-model.number="a.tenantUnits" type="number" class="input input-bordered input-sm" /></td>
              <td><input v-model.number="a.occupantCount" type="number" class="input input-bordered input-sm" /></td>
              <td><input v-model.number="a.occupancyDays" type="number" class="input input-bordered input-sm" /></td>
              <td><button class="btn btn-sm btn-error" @click="removeRow(idx)">刪除</button></td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="flex gap-2 mt-3">
        <button class="btn" @click="addRow">新增列</button>
        <button class="btn btn-primary" @click="calculate">試算</button>
        <button class="btn btn-secondary" @click="saveBill">儲存帳單</button>
        <span class="text-error text-sm self-center">{{ error }}</span>
      </div>

      <div class="mt-3 text-sm" v-if="result">
        <p>每度單價：{{ result.unitPrice.toFixed(4) }}</p>
        <p>私電總額：{{ result.privateElectricityAmount.toFixed(2) }}</p>
        <p>公電總額：{{ result.publicElectricityAmount.toFixed(2) }}</p>
        <p>應繳總額：{{ result.payableAmount.toFixed(2) }}</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import api from '../services/api'

const noop = () => {}
const calcMode = ref(3)
const selectedExpenseBillIds = ref<number[]>([])
const electricityExpenseBills = ref<any[]>([])
const relatedContracts = ref<any[]>([])
const error = ref('')
const result = ref<any>(null)
const today = new Date().toISOString().slice(0, 10)
const form = ref<any>({ contractId: 0, billingStart: today, billingEnd: today, allocations: [] })

const selectedBills = computed(() => electricityExpenseBills.value.filter((x:any)=>selectedExpenseBillIds.value.includes(x.id)))
const aggregatedAmount = computed(() => selectedBills.value.reduce((s:number,b:any)=>s + Number(b.amount || 0), 0))
const aggregatedUnits = computed(() => selectedBills.value.reduce((s:number,b:any)=>s + Number(b.usageUnits || 0), 0))

const addRow = () => form.value.allocations.push({ tenantId: null, tenantName: '', tenantUnits: 0, occupantCount: 1, occupancyDays: 30 })
const removeRow = (idx:number) => form.value.allocations.splice(idx,1)

const loadExpenseBills = async () => {
  const { data } = await api.get('/expenses', {
    params: {
      startDateUtc: new Date('2000-01-01T00:00:00Z').toISOString(),
      endDateUtc: new Date('2099-12-31T00:00:00Z').toISOString()
    }
  })
  electricityExpenseBills.value = data.filter((x: any) => x.category === 2)
}

const toggleBill = async (bill: any) => {
  const exists = selectedExpenseBillIds.value.includes(bill.id)
  if (exists) {
    selectedExpenseBillIds.value = selectedExpenseBillIds.value.filter(x => x !== bill.id)
  } else {
    if (selectedBills.value.length > 0) {
      const first = selectedBills.value[0]
      const samePeriod = first.billingStartUtc?.slice(0,10) === bill.billingStartUtc?.slice(0,10) &&
        first.billingEndUtc?.slice(0,10) === bill.billingEndUtc?.slice(0,10)
      if (!samePeriod) {
        error.value = '多錶計算僅可選擇同期帳單'
        return
      }
    }
    selectedExpenseBillIds.value.push(bill.id)
  }

  error.value = ''
  const first = selectedBills.value[0]
  if (!first) {
    relatedContracts.value = []
    form.value.contractId = 0
    form.value.allocations = []
    return
  }
  form.value.billingStart = first.billingStartUtc?.slice(0,10)
  form.value.billingEnd = first.billingEndUtc?.slice(0,10)

  const unitIds = [...new Set(selectedBills.value.map((x: any) => x.propertyUnitId).filter((x: any) => !!x))]
  const relMap = new Map<number, any>()
  for (const uid of unitIds) {
    const { data } = await api.get('/contracts', { params: { propertyUnitId: uid } })
    for (const c of data) relMap.set(c.id, c)
  }
  const rel = [...relMap.values()]
  relatedContracts.value = rel
  form.value.contractId = rel.length ? rel[0].id : 0
  form.value.allocations = rel.map((c: any) => ({
    tenantId: c.tenantId,
    tenantName: c.tenant?.name,
    tenantUnits: 0,
    occupantCount: c.occupantCount || 1,
    occupancyDays: 30
  }))
}

const validate = () => {
  if (!calcMode.value) return '請選擇計算方式'
  if (!selectedExpenseBillIds.value.length) return '請選擇至少一張電費帳單'
  if (calcMode.value === 3 && aggregatedUnits.value <= 0) return '多錶計算需有總度數'
  if (!form.value.allocations.length) return '請確認租客名單'
  return ''
}

const calculate = async () => {
  error.value = validate()
  if (error.value) return
  const payload = {
    ruleType: calcMode.value,
    billAmount: aggregatedAmount.value,
    totalUnits: aggregatedUnits.value,
    tenants: form.value.allocations.map((x: any) => ({ tenantUnits: x.tenantUnits, occupantCount: x.occupantCount, occupancyDays: x.occupancyDays }))
  }
  const { data } = await api.post('/electricity/calculate', payload)
  result.value = data
}

const saveBill = async () => {
  error.value = validate()
  if (error.value) return
  await api.post('/electricity/bills', {
    contractId: form.value.contractId,
    ruleType: calcMode.value,
    billingStartUtc: new Date(`${form.value.billingStart}T00:00:00Z`).toISOString(),
    billingEndUtc: new Date(`${form.value.billingEnd}T00:00:00Z`).toISOString(),
    totalAmount: aggregatedAmount.value,
    totalUnits: aggregatedUnits.value,
    unitPrice: aggregatedUnits.value === 0 ? 0 : aggregatedAmount.value / aggregatedUnits.value,
    allocations: form.value.allocations.map((x: any) => ({ tenantId: x.tenantId, tenantUnits: x.tenantUnits, occupantCount: x.occupantCount, occupancyDays: x.occupancyDays }))
  })
}

onMounted(loadExpenseBills)
</script>
