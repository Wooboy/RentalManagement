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

        <div></div>

        <div></div>
      </div>

      <div class="overflow-x-auto mb-3">
        <p class="text-sm mb-2">2. 選擇電費帳單（可多選）</p>
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
        <p class="text-sm mb-2">3. 關聯租約（自動帶入，可調整）</p>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-2 mb-2">
          <label class="form-control">
            <span class="label-text mb-1">新增列：從租約選擇房間</span>
            <select v-model="selectedContractForAdd" class="select select-bordered">
              <option value="">請選擇租約/房間</option>
              <option v-for="c in relatedContractRooms" :key="c.key" :value="c.key">{{ c.label }}</option>
            </select>
          </label>
          <div class="form-control justify-end">
            <button class="btn mt-6" @click="addRowFromContract">新增列</button>
          </div>
        </div>
        <table class="table table-zebra">
          <thead><tr><th>租約</th><th>歸屬帳單</th><th>本期用電度數</th><th>人數</th><th>入住天數</th><th></th></tr></thead>
          <tbody>
            <template v-for="(a, idx) in form.allocations" :key="idx">
              <tr>
                <td>{{ a.contractLabel || '-' }}</td>
                <td>
                  <select v-model.number="a.expenseBillId" class="select select-bordered select-sm">
                    <option :value="0">請選擇</option>
                    <option v-for="b in selectedBills" :key="b.id" :value="b.id">
                      #{{ b.id }} ({{ b.propertyUnitName || '-' }})
                    </option>
                  </select>
                </td>
                <td>
                  <div class="flex gap-1 items-center">
                    <input v-model.number="a.tenantUnits" type="number" class="input input-bordered input-sm" />
                    <button class="btn btn-xs" @click="openMeterEditor(idx)">編輯錶數/入住</button>
                  </div>
                </td>
                <td><input v-model.number="a.occupantCount" type="number" class="input input-bordered input-sm" /></td>
                <td><input v-model.number="a.occupancyDays" type="number" class="input input-bordered input-sm" /></td>
                <td><button class="btn btn-sm btn-error" @click="removeRow(idx)">刪除</button></td>
              </tr>
              <tr v-if="a && a.calcDetail">
                <td colspan="6" class="text-sm bg-base-200">
                  <div>私電度數：{{ a.calcDetail.privateUnitsText }}</div>
                  <div>私電費：{{ a.calcDetail.privateAmountText }}</div>
                  <div>公電費：{{ a.calcDetail.publicAmountText }}</div>
                  <div>合計：{{ a.calcDetail.totalText }}</div>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>

      <div class="flex gap-2 mt-3">
        <button class="btn btn-primary" @click="calculate">試算</button>
        <button class="btn btn-secondary" @click="saveBill">儲存帳單</button>
        <span class="text-error text-sm self-center">{{ error }}</span>
      </div>

      <div class="mt-3 text-sm" v-if="result">
        <p>每度單價：{{ result.unitPrice.toFixed(2) }}</p>
        <p>私電總額：{{ result.privateElectricityAmount.toFixed(2) }}</p>
        <p>公電總額：{{ result.publicElectricityAmount.toFixed(2) }}</p>
        <p>應繳總額：{{ result.payableAmount.toFixed(2) }}</p>
      </div>
    </div>

    <dialog class="modal" :class="{ 'modal-open': meterEditor.open }">
      <div class="modal-box">
        <h3 class="font-bold text-lg mb-3">編輯錶數</h3>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-2">
          <label class="form-control"><span class="label-text mb-1">上期讀表日（帳期起日）</span><input v-model="meterEditor.prevDate" type="date" max="2099-12-31" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">本期讀表日（帳期迄日）</span><input v-model="meterEditor.currentDate" type="date" max="2099-12-31" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">前一期度數</span><input v-model.number="meterEditor.prevUnits" type="number" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">現在錶數</span><input v-model.number="meterEditor.currentMeter" type="number" class="input input-bordered" /></label>
        </div>
        <p class="text-sm mt-2">用電度數 = 現在錶數 - 前一期度數</p>
        <div class="modal-action">
          <button class="btn" @click="closeMeterEditor">取消</button>
          <button class="btn btn-primary" @click="saveMeterEditor">儲存</button>
        </div>
      </div>
    </dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import api from '../services/api'

const calcMode = ref(3)
const selectedExpenseBillIds = ref<number[]>([])
const electricityExpenseBills = ref<any[]>([])
const relatedContracts = ref<any[]>([])
const relatedContractRooms = ref<any[]>([])
const selectedContractForAdd = ref<string>('')
const error = ref('')
const result = ref<any>(null)
const today = new Date().toISOString().slice(0, 10)
const form = ref<any>({ contractId: 0, billingStart: today, billingEnd: today, allocations: [] })
const meterEditor = ref<any>({ open: false, rowIndex: -1, prevDate: '', currentDate: '', prevUnits: 0, currentMeter: 0 })
const round2 = (n:number) => Math.round((Number(n || 0) + Number.EPSILON) * 100) / 100
const trunc2 = (n:number) => Math.floor(Number(n || 0) * 100) / 100
const md = (v?:string) => {
  if (!v) return '-'
  const d = v.slice(5,10).split('-')
  if (d.length !== 2) return v
  return `${Number(d[0])}/${Number(d[1])}`
}

const selectedBills = computed(() => electricityExpenseBills.value.filter((x:any)=>selectedExpenseBillIds.value.includes(x.id)))
const selectedBillMap = computed(() => new Map<number, any>(selectedBills.value.map((b:any)=>[b.id,b])))
const aggregatedAmount = computed(() => selectedBills.value.reduce((s:number,b:any)=>s + Number(b.amount || 0), 0))
const aggregatedUnits = computed(() => selectedBills.value.reduce((s:number,b:any)=>s + Number(b.usageUnits || 0), 0))
const periodDays = computed(() => {
  if (!form.value.billingStart || !form.value.billingEnd) return 0
  const start = new Date(`${form.value.billingStart}T00:00:00Z`)
  const end = new Date(`${form.value.billingEnd}T00:00:00Z`)
  if (end < start) return 0
  return Math.floor((end.getTime() - start.getTime()) / 86400000) + 1
})
const resolveBillForAllocation = (a:any) => {
  return selectedBillMap.value.get(Number(a?.expenseBillId || 0))
}
const findBillByRoom = (roomId:number) => selectedBills.value.find((b:any) => Number(b.propertyRoomId || 0) === Number(roomId))
const getLatestSelectedBill = () => {
  const lastId = selectedExpenseBillIds.value[selectedExpenseBillIds.value.length - 1]
  return selectedBillMap.value.get(Number(lastId || 0)) || selectedBills.value[selectedBills.value.length - 1]
}

const addRowFromContract = () => {
  error.value = ''
  const pick = relatedContractRooms.value.find((x:any) => x.key === selectedContractForAdd.value)
  if (!pick) {
    error.value = '請先選擇租約/房間再新增'
    return
  }
  form.value.allocations.push({
    contractId: pick.contractId,
    propertyRoomId: pick.propertyRoomId,
    tenantId: pick.tenantId,
    tenantName: pick.tenantName || '',
    contractLabel: pick.label,
    expenseBillId: findBillByRoom(Number(pick.propertyRoomId || 0))?.id || selectedBills.value[0]?.id || 0,
    tenantUnits: 0,
    occupantCount: pick.occupantCount || 1,
    occupancyDays: periodDays.value,
    occupancyStartDate: form.value.billingStart,
    occupancyEndDate: form.value.billingEnd,
    calcDetail: null
  })
}
const removeRow = (idx:number) => form.value.allocations.splice(idx,1)

const loadExpenseBills = async () => {
  const { data } = await api.get('/expenses', {
    params: {
      startDateUtc: new Date('2000-01-01T00:00:00Z').toISOString(),
      endDateUtc: new Date('2099-12-31T00:00:00Z').toISOString()
    }
  })
  electricityExpenseBills.value = data.filter((x: any) => x.category === 2)
    .sort((a:any,b:any) => {
      const aDate = String(a.occurredAtUtc || '')
      const bDate = String(b.occurredAtUtc || '')
      if (aDate !== bDate) return aDate.localeCompare(bDate)
      const au = Number(a.propertyUnitId || 0)
      const bu = Number(b.propertyUnitId || 0)
      if (au !== bu) return au - bu
      return Number(a.id || 0) - Number(b.id || 0)
    })
}

const toggleBill = async (bill: any) => {
  const alreadySelected = selectedExpenseBillIds.value.includes(bill.id)
  const selectingNew = !alreadySelected
  const newlySelectedBillId = Number(bill.id)
  const newlySelectedRoomId = Number(bill.propertyRoomId || 0)
  if (alreadySelected) {
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
    relatedContractRooms.value = []
    selectedContractForAdd.value = ''
    form.value.contractId = 0
    form.value.allocations = []
    return
  }
  form.value.billingStart = first.billingStartUtc?.slice(0,10)
  form.value.billingEnd = first.billingEndUtc?.slice(0,10)

  const unitIds = [...new Set(selectedBills.value.map((x: any) => x.propertyUnitId).filter((x: any) => !!x))]
  const roomIds = [...new Set(selectedBills.value.map((x: any) => x.propertyRoomId).filter((x: any) => !!x))]
  const relMap = new Map<number, any>()
  for (const uid of unitIds) {
    const { data } = await api.get('/contracts', { params: { propertyUnitId: uid } })
    for (const c of data) relMap.set(c.id, c)
  }
  for (const rid of roomIds) {
    const { data } = await api.get('/contracts', { params: { propertyRoomId: rid } })
    for (const c of data) relMap.set(c.id, c)
  }
  const rel = [...relMap.values()]
  relatedContracts.value = rel
  relatedContractRooms.value = rel.flatMap((c:any) => {
    const rooms = Array.isArray(c.rooms) ? c.rooms : []
    if (!rooms.length) {
      return [{
        key: `${c.id}:0`,
        contractId: c.id,
        propertyRoomId: 0,
        tenantId: c.tenantId,
        tenantName: c.tenant?.name || '',
        occupantCount: c.occupantCount || 1,
        label: `${c.contractNo} - ${c.tenant?.name || '-'}`
      }]
    }
    return rooms.map((r:any) => ({
      key: `${c.id}:${r.propertyRoomId}`,
      contractId: c.id,
      propertyRoomId: r.propertyRoomId,
      tenantId: c.tenantId,
      tenantName: c.tenant?.name || '',
      occupantCount: c.occupantCount || 1,
      label: `${c.contractNo} - ${c.tenant?.name || '-'} / ${r.roomCode || ''}${r.roomName ? ` ${r.roomName}` : ''}`
    }))
  })
  selectedContractForAdd.value = relatedContractRooms.value[0]?.key || ''
  form.value.contractId = rel.length ? rel[0].id : 0
  if (!form.value.allocations.length) {
    form.value.allocations = relatedContractRooms.value.map((x:any) => ({
      contractId: x.contractId,
      propertyRoomId: x.propertyRoomId,
      tenantId: x.tenantId,
      tenantName: x.tenantName,
      contractLabel: x.label,
      expenseBillId: (selectingNew && newlySelectedRoomId > 0 && Number(x.propertyRoomId || 0) === newlySelectedRoomId)
        ? newlySelectedBillId
        : (findBillByRoom(Number(x.propertyRoomId || 0))?.id || getLatestSelectedBill()?.id || 0),
      tenantUnits: 0,
      occupantCount: x.occupantCount || 1,
      occupancyDays: periodDays.value,
      occupancyStartDate: form.value.billingStart,
      occupancyEndDate: form.value.billingEnd,
      calcDetail: null
    }))
  } else {
    const existingKeys = new Set(form.value.allocations.map((a:any) => `${a.contractId}:${a.propertyRoomId}`))
    const missing = relatedContractRooms.value
      .filter((x:any) => !existingKeys.has(`${x.contractId}:${x.propertyRoomId}`))
      .map((x:any) => ({
        contractId: x.contractId,
        propertyRoomId: x.propertyRoomId,
        tenantId: x.tenantId,
        tenantName: x.tenantName,
        contractLabel: x.label,
        expenseBillId: (selectingNew && newlySelectedRoomId > 0 && Number(x.propertyRoomId || 0) === newlySelectedRoomId)
          ? newlySelectedBillId
          : (findBillByRoom(Number(x.propertyRoomId || 0))?.id || getLatestSelectedBill()?.id || 0),
        tenantUnits: 0,
        occupantCount: x.occupantCount || 1,
        occupancyDays: periodDays.value,
        occupancyStartDate: form.value.billingStart,
        occupancyEndDate: form.value.billingEnd,
        calcDetail: null
      }))
    if (missing.length) form.value.allocations.push(...missing)
  }

  if (selectingNew && newlySelectedRoomId > 0) {
    for (const row of form.value.allocations) {
      if (Number(row.propertyRoomId || 0) === newlySelectedRoomId) {
        row.expenseBillId = newlySelectedBillId
      }
    }
  }
}

const openMeterEditor = (idx:number) => {
  const row = form.value.allocations[idx]
  meterEditor.value = {
    open: true,
    rowIndex: idx,
    prevDate: (resolveBillForAllocation(row)?.billingStartUtc || form.value.billingStart)?.slice(0,10),
    currentDate: (resolveBillForAllocation(row)?.billingEndUtc || form.value.billingEnd)?.slice(0,10),
    prevUnits: Number(row.prevUnits || 0),
    currentMeter: Number(row.currentMeter || 0)
  }
}
const closeMeterEditor = () => { meterEditor.value.open = false }
const saveMeterEditor = () => {
  const i = meterEditor.value.rowIndex
  if (i < 0) return
  const row = form.value.allocations[i]
  const usage = Number(meterEditor.value.currentMeter || 0) - Number(meterEditor.value.prevUnits || 0)
  row.prevReadingDate = meterEditor.value.prevDate || ''
  row.currentReadingDate = meterEditor.value.currentDate || ''
  row.prevUnits = Number(meterEditor.value.prevUnits || 0)
  row.currentMeter = Number(meterEditor.value.currentMeter || 0)
  row.tenantUnits = round2(usage < 0 ? 0 : usage)
  closeMeterEditor()
}

const validate = () => {
  if (!calcMode.value) return '請選擇計算方式'
  if (!selectedExpenseBillIds.value.length) return '請選擇至少一張電費帳單'
  if (calcMode.value === 3 && aggregatedUnits.value <= 0) return '多錶計算需有總度數'
  if (!form.value.allocations.length) return '請確認租客名單'
  if (form.value.allocations.some((x:any)=>!x.tenantId)) return '租客不可為空'
  if (form.value.allocations.some((x:any)=>!x.expenseBillId)) return '每筆租客需指定歸屬帳單'
  if (form.value.allocations.some((x:any)=>!selectedExpenseBillIds.value.includes(Number(x.expenseBillId)))) return '租客歸屬帳單必須在已勾選帳單內'
  if (!(form.value.contractId || relatedContracts.value[0]?.id)) return '找不到可用合約，請先確認帳單歸屬房源是否已有合約'
  return ''
}

const calculate = async () => {
  error.value = validate()
  if (error.value) return
  if (calcMode.value === 3) {
    const privateAmounts = form.value.allocations.map((x:any) => {
      const bill = resolveBillForAllocation(x)
      const amount = Number(bill?.amount || 0)
      const units = Number(bill?.usageUnits || 0)
      const unitPrice = units === 0 ? 0 : amount / units
      return round2(Number(x.tenantUnits || 0) * unitPrice)
    })
    const privateTotal = round2(privateAmounts.reduce((s:number,v:number)=>s+v,0))
    const publicTotal = round2(aggregatedAmount.value - privateTotal)
    const divisor = form.value.allocations.reduce((s:number,x:any)=>s + (Number(x.occupantCount || 0) * Number(x.occupancyDays || 0)),0)
    const averageDailyPrice = divisor === 0 ? 0 : round2(publicTotal / divisor)
    const payables = form.value.allocations.map((x:any,idx:number) => {
      const publicPart = round2(Number(x.occupantCount || 0) * Number(x.occupancyDays || 0) * averageDailyPrice)
      const payable = trunc2(privateAmounts[idx] + publicPart)
      const bill = resolveBillForAllocation(x)
      const billAmount = Number(bill?.amount || 0)
      const billUnits = Number(bill?.usageUnits || 0)
      const unitPrice = billUnits === 0 ? 0 : round2(billAmount / billUnits)
      x.calcDetail = {
        privateUnitsText: `本期度數(${md(x.currentReadingDate)}) - 上期度數(${md(x.prevReadingDate)}) = ${round2(Number(x.currentMeter || 0))} - ${round2(Number(x.prevUnits || 0))} = ${round2(Number(x.tenantUnits || 0))}度`,
        privateAmountText: `${round2(Number(x.tenantUnits || 0))}度 × 單價 ${unitPrice}元/度 = ${privateAmounts[idx]}元`,
        publicAmountText: `住${Number(x.occupancyDays || 0)}日 × ${Number(x.occupantCount || 0)}人 × ${averageDailyPrice}元/日 = ${publicPart}元`,
        totalText: `${privateAmounts[idx]} + ${publicPart} = ${payable}元`
      }
      return payable
    })
    result.value = {
      unitPrice: aggregatedUnits.value === 0 ? 0 : round2(aggregatedAmount.value / aggregatedUnits.value),
      privateElectricityAmount: privateTotal,
      publicElectricityAmount: publicTotal,
      payableAmount: trunc2(payables.reduce((s:number,v:number)=>s+v,0)),
      tenantPayables: payables
    }
    return
  }
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
    contractId: form.value.contractId || relatedContracts.value[0]?.id || 0,
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
