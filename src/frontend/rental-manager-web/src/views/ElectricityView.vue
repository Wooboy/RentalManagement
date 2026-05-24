<template>
  <div class="space-y-4">
    <div class="card bg-base-100 shadow p-4">
      <h2 class="text-lg font-bold mb-3">電費試算與入帳（多錶）</h2>

      <div class="grid grid-cols-1 md:grid-cols-4 gap-2 mb-3">
        <label class="form-control"><span class="label-text mb-1">合約</span><select v-model.number="form.contractId" class="select select-bordered">
          <option :value="0">選擇合約 *</option>
          <option v-for="c in contracts" :key="c.id" :value="c.id">{{ c.contractNo }} - {{ c.propertyName }}</option>
        </select></label>
        <label class="form-control"><span class="label-text mb-1">帳期起日</span><input v-model="form.billingStart" type="date" max="2099-12-31" class="input input-bordered" /></label>
        <label class="form-control"><span class="label-text mb-1">帳期迄日</span><input v-model="form.billingEnd" type="date" max="2099-12-31" class="input input-bordered" /></label>
        <label class="form-control"><span class="label-text mb-1">總金額</span><input v-model.number="form.totalAmount" type="number" class="input input-bordered" placeholder="請輸入" /></label>
        <label class="form-control"><span class="label-text mb-1">總度數</span><input v-model.number="form.totalUnits" type="number" class="input input-bordered" placeholder="請輸入" /></label>
      </div>

      <div class="overflow-x-auto">
        <table class="table table-zebra">
          <thead><tr><th>租客ID</th><th>租客度數</th><th>人數</th><th>入住天數</th><th></th></tr></thead>
          <tbody>
            <tr v-for="(a, idx) in form.allocations" :key="idx">
              <td><input v-model.number="a.tenantId" type="number" class="input input-bordered input-sm" /></td>
              <td><input v-model.number="a.tenantUnits" type="number" class="input input-bordered input-sm" /></td>
              <td><input v-model.number="a.occupantCount" type="number" class="input input-bordered input-sm" /></td>
              <td><input v-model.number="a.occupancyDays" type="number" class="input input-bordered input-sm" /></td>
              <td><button class="btn btn-sm btn-error" @click="removeRow(idx)">刪除</button></td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="flex gap-2 mt-3">
        <button class="btn" @click="addRow">新增分攤列</button>
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

    <div class="card bg-base-100 shadow p-4">
      <h3 class="font-bold mb-3">歷史電費帳單</h3>
      <div class="flex gap-2 mb-3">
        <label class="form-control"><span class="label-text mb-1">合約篩選</span><select v-model.number="billFilterContractId" class="select select-bordered">
          <option :value="0">全部合約</option>
          <option v-for="c in contracts" :key="c.id" :value="c.id">{{ c.contractNo }}</option>
        </select></label>
        <button class="btn" @click="loadBills">查詢</button>
      </div>
      <table class="table table-zebra">
        <thead><tr><th>ID</th><th>合約</th><th>帳期</th><th>總金額</th><th>總度數</th><th>每度</th><th>應繳總額</th><th></th></tr></thead>
        <tbody>
          <tr v-for="b in bills" :key="b.id">
            <td>{{ b.id }}</td>
            <td>{{ b.contractNo }}</td>
            <td>{{ b.billingStartUtc.slice(0,10) }} ~ {{ b.billingEndUtc.slice(0,10) }}</td>
            <td>{{ b.totalAmount }}</td>
            <td>{{ b.totalUnits }}</td>
            <td>{{ b.unitPrice }}</td>
            <td>{{ b.payableTotalAmount }}</td>
            <td class="flex gap-2 justify-end">
              <button class="btn btn-sm" @click="loadBillDetail(b.id)">明細</button>
              <button class="btn btn-sm btn-accent" :disabled="b.chargesCreated" @click="createCharges(b.id, 'split')">
                {{ b.chargesCreated ? '已轉入' : '分筆轉入' }}
              </button>
              <button class="btn btn-sm" :disabled="b.chargesCreated" @click="createCharges(b.id, 'merged')">合併轉入</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="card bg-base-100 shadow p-4" v-if="selectedBillDetail">
      <h3 class="font-bold mb-3">帳單明細 #{{ selectedBillDetail.id }}</h3>
      <p class="text-sm mb-2">合約：{{ selectedBillDetail.contractNo }}，每度：{{ selectedBillDetail.unitPrice }}</p>
      <table class="table table-zebra">
        <thead><tr><th>租客</th><th>度數</th><th>私電</th><th>公電</th><th>應繳</th></tr></thead>
        <tbody>
          <tr v-for="a in selectedBillDetail.allocations" :key="a.id">
            <td>{{ a.tenantName || a.tenantId || '-' }}</td>
            <td>{{ a.tenantUnits }}</td>
            <td>{{ a.privateAmount }}</td>
            <td>{{ a.publicAmount }}</td>
            <td>{{ a.payableAmount }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import api from '../services/api'
const router = useRouter()

const contracts = ref<any[]>([])
const bills = ref<any[]>([])
const billFilterContractId = ref(0)
const error = ref('')
const result = ref<any>(null)
const selectedBillDetail = ref<any>(null)

const today = new Date().toISOString().slice(0, 10)
const form = ref<any>({
  contractId: 0,
  billingStart: today,
  billingEnd: today,
  totalAmount: 0,
  totalUnits: 0,
  allocations: [{ tenantId: null, tenantUnits: 0, occupantCount: 1, occupancyDays: 30 }]
})

const addRow = () => form.value.allocations.push({ tenantId: null, tenantUnits: 0, occupantCount: 1, occupancyDays: 30 })
const removeRow = (idx: number) => form.value.allocations.splice(idx, 1)

const validate = () => {
  if (!form.value.contractId) return '請選擇合約'
  if (!form.value.billingStart || !form.value.billingEnd) return '請選擇帳期'
  if (form.value.totalAmount < 0 || form.value.totalUnits < 0) return '金額與度數需 >= 0'
  if (!form.value.allocations.length) return '至少要有一筆分攤資料'
  return ''
}

const calcPayload = () => ({
  ruleType: 3,
  billAmount: form.value.totalAmount,
  totalUnits: form.value.totalUnits,
  tenants: form.value.allocations.map((x: any) => ({ tenantUnits: x.tenantUnits, occupantCount: x.occupantCount, occupancyDays: x.occupancyDays }))
})

const calculate = async () => {
  error.value = validate()
  if (error.value) return
  const { data } = await api.post('/electricity/calculate', calcPayload())
  result.value = data
}

const saveBill = async () => {
  error.value = validate()
  if (error.value) return

  const totalAmount = form.value.totalAmount
  const totalUnits = form.value.totalUnits
  const unitPrice = totalUnits === 0 ? 0 : totalAmount / totalUnits

  await api.post('/electricity/bills', {
    contractId: form.value.contractId,
    ruleType: 3,
    billingStartUtc: new Date(`${form.value.billingStart}T00:00:00Z`).toISOString(),
    billingEndUtc: new Date(`${form.value.billingEnd}T00:00:00Z`).toISOString(),
    totalAmount,
    totalUnits,
    unitPrice,
    allocations: form.value.allocations
  })

  await loadBills()
}

const loadContracts = async () => {
  const { data } = await api.get('/contracts')
  contracts.value = data
}

const loadBills = async () => {
  const params = billFilterContractId.value ? { contractId: billFilterContractId.value } : undefined
  const { data } = await api.get('/electricity/bills', { params })
  bills.value = data
}

const loadBillDetail = async (billId: number) => {
  const { data } = await api.get(`/electricity/bills/${billId}`)
  selectedBillDetail.value = data
}

const createCharges = async (billId: number, mode: 'split' | 'merged') => {
  const bill = bills.value.find((x: any) => x.id === billId)
  const params = mode === 'merged' ? { mode: 'merged' } : undefined
  await api.post(`/electricity/bills/${billId}/create-charges`, null, { params })
  await loadBills()
  if (bill) {
    const d = new Date(bill.billingStartUtc)
    await router.push({ path: '/charges', query: { year: String(d.getUTCFullYear()), month: String(d.getUTCMonth() + 1) } })
  }
}

onMounted(async () => {
  await Promise.all([loadContracts(), loadBills()])
})
</script>
