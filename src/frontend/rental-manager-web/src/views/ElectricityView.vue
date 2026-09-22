<template>
  <div class="space-y-4">
    <PageHeader title="電費試算" subtitle="選擇帳單並試算分攤後，儲存並轉入應收" />
    <div class="card bg-base-100 border border-base-300 shadow-sm p-4">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-2 mb-3">
        <label class="form-control"><span class="label-text mb-1">1. 計算方式</span>
          <select v-model.number="calcMode" class="select select-bordered">
            <option :value="1">依度數計算</option>
            <option :value="3">多錶計算（各錶單價）</option>
            <option :value="4">多錶平均單價（多帳單加總）</option>
          </select>
        </label>
      </div>

      <div class="overflow-x-auto mb-3">
        <p class="text-sm mb-2">2. 選擇電費帳單（{{ calcMode === 1 ? '僅可單選' : '可多選' }}）</p>
        <table class="table">
          <thead><tr><th></th><th>發生日期</th><th>帳期</th><th>金額</th><th>度數</th><th>房源/房間</th></tr></thead>
          <tbody>
            <tr v-for="b in electricityExpenseBills" :key="b.id">
              <td><input type="checkbox" class="checkbox checkbox-sm" :checked="selectedExpenseBillIds.includes(b.id)" @change="toggleBill(b)" /></td>
              <td>{{ b.occurredAtUtc?.slice(0,10) || '-' }}</td>
              <td>{{ b.billingStartUtc?.slice(0,10) }} ~ {{ b.billingEndUtc?.slice(0,10) }}</td>
              <td>{{ b.amount }}</td>
              <td>{{ b.usageUnits ?? '-' }}</td>
              <td>{{ b.propertyUnitName || '-' }} / {{ b.propertyRoomName || '-' }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-4 gap-2 mb-3">
        <label class="form-control"><span class="label-text mb-1">帳期起日</span><input :value="billingStart" type="date" max="2099-12-31" class="input input-bordered" disabled /></label>
        <label class="form-control"><span class="label-text mb-1">帳期迄日</span><input :value="billingEnd" type="date" max="2099-12-31" class="input input-bordered" disabled /></label>
        <label class="form-control"><span class="label-text mb-1">帳單總額（加總）</span><input :value="aggregatedAmount" type="number" class="input input-bordered" disabled /></label>
        <label class="form-control"><span class="label-text mb-1">總度數（加總）</span><input :value="aggregatedUnits" type="number" class="input input-bordered" disabled /></label>
      </div>

      <div v-if="calcMode === 1" class="grid grid-cols-1 md:grid-cols-4 gap-2 mb-3">
        <label class="form-control">
          <span class="label-text mb-1">依度數計算單價（元/度）</span>
          <input v-model.number="manualUnitPrice" type="number" class="input input-bordered" />
        </label>
      </div>

      <div class="overflow-x-auto">
        <p class="text-sm mb-2">3. 帳期內居住租客（依合約與抄表自動帶入）</p>
        <table class="table">
          <thead><tr><th>分攤對象</th><th>租客/合約</th><th>房間</th><th>居住期間</th><th>入住天數</th><th>人數</th><th>天數×人數</th><th>起訖度數</th><th>用電度數</th><th>歸屬帳單</th></tr></thead>
          <tbody>
            <tr v-for="(a, idx) in allocations" :key="`${a.expenseBillId}-${a.contractId}-${idx}`">
              <td>{{ a.targetName }}</td>
              <td>{{ partyText(a) }}</td>
              <td>{{ a.propertyUnitName || '-' }} / {{ a.propertyRoomName || '-' }}</td>
              <td>{{ a.occupancyStartUtc?.slice(0,10) }} ~ {{ a.occupancyEndUtc?.slice(0,10) }}</td>
              <td>{{ a.occupancyDays }}</td>
              <td>{{ a.occupantCount }}</td>
              <td>{{ a.occupancyWeight }}</td>
              <td>{{ meterText(a) }}</td>
              <td>{{ a.tenantUnits }}</td>
              <td>#{{ a.expenseBillId }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="isPreviewLoading" class="mt-3 text-sm text-base-content/70">正在帶入合約與抄表資料...</div>
      <div v-else-if="selectedExpenseBillIds.length && !allocations.length && !previewWarnings.length" class="mt-3 alert alert-warning text-sm">
        <span>選取的帳單目前沒有帶入可計算的合約，請檢查合約房間、帳期，以及是否已有對應抄表資料。</span>
      </div>

      <div v-if="previewWarnings.length" class="mt-3 alert alert-warning text-sm whitespace-pre-line">
        <div>
          <div v-for="(warning, idx) in previewWarnings" :key="idx">{{ warning }}</div>
        </div>
      </div>

      <div class="flex gap-2 mt-3">
        <button class="btn btn-primary" @click="calculate">試算</button>
        <button class="btn btn-secondary" @click="saveBill">儲存帳單</button>
        <span class="text-error text-sm self-center">{{ error }}</span>
        <span class="text-success text-sm self-center">{{ notice }}</span>
      </div>

      <div class="mt-3 text-sm" v-if="result">
        <p>每度單價：{{ result.unitPrice.toFixed(2) }}</p>
        <p>私電總額：{{ result.privateElectricityAmount.toFixed(2) }}</p>
        <p>公電總額：{{ result.publicElectricityAmount.toFixed(2) }}</p>
        <p>應繳總額：{{ result.payableAmount.toFixed(2) }}</p>
        <p v-if="calcMode !== 1">租客應收總額：{{ trunc2(tenantPayableTotal).toFixed(2) }}</p>
        <p v-if="calcMode !== 1">房東自付總額：{{ trunc2(landlordPayableTotal).toFixed(2) }}</p>
        <div v-if="calcMode !== 1" class="mt-2 space-y-1">
          <p>公電總額 {{ result.publicElectricityAmount.toFixed(2) }} = 應繳總額 {{ aggregatedAmount.toFixed(2) }} - 私電總額 {{ result.privateElectricityAmount.toFixed(2) }}</p>
          <p>公電單價 {{ publicUnitPriceText }} = 公電總額 {{ result.publicElectricityAmount.toFixed(2) }} / (本期日數 {{ billingDays }} × 居住總人數 {{ totalOccupantsText }})</p>
        </div>
      </div>

      <div v-if="result && allocations.length" class="overflow-x-auto mt-3">
        <table class="table">
          <thead><tr><th>分攤對象</th><th>租客/合約</th><th>私電明細</th><th>公電明細</th><th>合計</th></tr></thead>
          <tbody>
            <tr v-for="(a, idx) in allocations" :key="`detail-${a.expenseBillId}-${a.contractId}-${idx}`">
              <td>{{ a.targetName }}</td>
              <td>{{ partyText(a) }}</td>
              <td class="whitespace-pre-line">{{ a.calcDetail?.privateAmountText || '-' }}</td>
              <td>{{ a.calcDetail?.publicAmountText || '-' }}</td>
              <td>{{ a.calcDetail?.totalText || '-' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import api from '../services/api'
import PageHeader from '../components/PageHeader.vue'

const calcMode = ref(3)
const selectedExpenseBillIds = ref<number[]>([])
const electricityExpenseBills = ref<any[]>([])
const allocations = ref<any[]>([])
const previewWarnings = ref<string[]>([])
const error = ref('')
const notice = ref('')
const result = ref<any>(null)
const manualUnitPrice = ref<number>(0)
const isPreviewLoading = ref(false)
let previewRequestSeq = 0
const round2 = (n:number) => Math.round((Number(n || 0) + Number.EPSILON) * 100) / 100
const trunc2 = (n:number) => Math.floor(Number(n || 0) * 100) / 100
const trunc0 = (n:number) => Math.floor(Number(n || 0))

const selectedBills = computed(() => electricityExpenseBills.value.filter((x:any)=>selectedExpenseBillIds.value.includes(x.id)))
const selectedBillMap = computed(() => new Map<number, any>(selectedBills.value.map((b:any)=>[b.id,b])))
const aggregatedAmount = computed(() => selectedBills.value.reduce((s:number,b:any)=>s + Number(b.amount || 0), 0))
const aggregatedUnits = computed(() => selectedBills.value.reduce((s:number,b:any)=>s + Number(b.usageUnits || 0), 0))
const billingStart = computed(() => selectedBills.value[0]?.billingStartUtc?.slice(0,10) || '')
const billingEnd = computed(() => selectedBills.value[0]?.billingEndUtc?.slice(0,10) || '')
const billingDays = computed(() => {
  if (!billingStart.value || !billingEnd.value) return 0
  const start = new Date(`${billingStart.value}T00:00:00Z`)
  const end = new Date(`${billingEnd.value}T00:00:00Z`)
  return end < start ? 0 : Math.floor((end.getTime() - start.getTime()) / 86400000) + 1
})
const occupancyWeightTotal = computed(() => allocations.value.reduce((s:number, x:any) => s + Number(x.occupancyWeight || 0), 0))
const totalOccupants = computed(() => billingDays.value === 0 ? 0 : occupancyWeightTotal.value / billingDays.value)
const totalOccupantsText = computed(() => round2(totalOccupants.value).toFixed(2))
const landlordPayableTotal = computed(() => allocations.value
  .filter((x:any) => Number(x.targetType) === 2)
  .reduce((s:number, x:any) => s + Number(x.calcDetail?.payableAmount || 0), 0))
const tenantPayableTotal = computed(() => allocations.value
  .filter((x:any) => Number(x.targetType) === 1)
  .reduce((s:number, x:any) => s + Number(x.calcDetail?.payableAmount || 0), 0))
const publicUnitPriceText = computed(() => {
  if (!result.value || calcMode.value === 1 || occupancyWeightTotal.value === 0) return '0.00'
  return round2(Number(result.value.publicElectricityAmount || 0) / occupancyWeightTotal.value).toFixed(2)
})

const meterText = (row:any) => {
  if (row.meterStart == null || row.meterEnd == null) return '-'
  return `${round2(Number(row.meterStart))} → ${round2(Number(row.meterEnd))}`
}
const partyText = (row:any) => Number(row.targetType) === 2 ? '房東自付' : `${row.tenantName || '-'} / ${row.contractNo || '-'}`
const formatDate = (value:any) => value ? String(value).slice(0, 10) : '-'
const buildPrivateUsageLine = (row:any) => {
  if (row.meterStart == null || row.meterEnd == null) return `${round2(Number(row.tenantUnits || 0))}度`
  return `本期度數(${formatDate(row.meterEndDateUtc)}) ${round2(Number(row.meterEnd))} - 上期度數(${formatDate(row.meterStartDateUtc)}) ${round2(Number(row.meterStart))} = ${round2(Number(row.tenantUnits || 0))}度`
}

const loadExpenseBills = async () => {
  const { data } = await api.get('/expenses', {
    params: {
      startDateUtc: new Date('2000-01-01T00:00:00Z').toISOString(),
      endDateUtc: new Date('2099-12-31T00:00:00Z').toISOString()
    }
  })
  electricityExpenseBills.value = data.filter((x: any) => x.category === 2 && Number(x.splitStatus || 1) === 1)
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

const resetPreview = () => {
  allocations.value = []
  previewWarnings.value = []
  result.value = null
}

const loadPreview = async () => {
  const requestSeq = ++previewRequestSeq
  resetPreview()
  if (!selectedExpenseBillIds.value.length) {
    isPreviewLoading.value = false
    return
  }
  isPreviewLoading.value = true
  try {
    const { data } = await api.post('/electricity/preview-from-expenses', { expenseBillIds: [...selectedExpenseBillIds.value] })
    if (requestSeq !== previewRequestSeq) return

    previewWarnings.value = Array.isArray(data?.warnings) ? data.warnings : []
    allocations.value = (Array.isArray(data?.allocations) ? data.allocations : []).map((x:any) => ({
      ...x,
      occupancyWeight: Number(x.occupancyDays || 0) * Number(x.occupantCount || 0),
      calcDetail: null
    }))

    if (!allocations.value.length && previewWarnings.value.length) {
      error.value = '選取帳單沒有帶入可計算的合約，請先確認警示內容。'
    }
  } catch (e:any) {
    if (requestSeq !== previewRequestSeq) return
    const message = e?.response?.data || e?.message || '帶入帳單資料失敗'
    error.value = typeof message === 'string' ? message : JSON.stringify(message)
  } finally {
    if (requestSeq === previewRequestSeq) {
      isPreviewLoading.value = false
    }
  }
}

const toggleBill = async (bill: any) => {
  error.value = ''
  notice.value = ''
  result.value = null
  const alreadySelected = selectedExpenseBillIds.value.includes(bill.id)
  if (alreadySelected) {
    selectedExpenseBillIds.value = selectedExpenseBillIds.value.filter(x => x !== bill.id)
    await loadPreview()
    return
  }

  if (calcMode.value === 1) {
    selectedExpenseBillIds.value = [bill.id]
    await loadPreview()
    return
  }

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
  await loadPreview()
}

const validate = () => {
  if (!calcMode.value) return '請選擇計算方式'
  if (!selectedExpenseBillIds.value.length) return '請選擇至少一張電費帳單'
  if (calcMode.value === 1 && selectedExpenseBillIds.value.length > 1) return '此計算方式僅可選擇一張帳單'
  if (calcMode.value === 1 && Number(manualUnitPrice.value || 0) <= 0) return '依度數計算請輸入單價'
  if (!allocations.value.length) return '帳期內找不到可分攤的租客資料'
  return ''
}

const calculate = async () => {
  notice.value = ''
  error.value = validate()
  if (error.value) return

  if (calcMode.value === 3) {
    const privateAmounts = allocations.value.map((x:any) => {
      const bill = selectedBillMap.value.get(Number(x.expenseBillId || 0))
      const amount = Number(bill?.amount || 0)
      const units = Number(bill?.usageUnits || 0)
      const unitPrice = units === 0 ? 0 : amount / units
      return round2(Number(x.tenantUnits || 0) * unitPrice)
    })
    const privateTotal = round2(privateAmounts.reduce((s:number,v:number)=>s+v,0))
    const publicTotal = round2(aggregatedAmount.value - privateTotal)
    const divisor = allocations.value.reduce((s:number,x:any)=>s + Number(x.occupancyWeight || 0),0)
    const averageDailyPrice = divisor === 0 ? 0 : round2(publicTotal / divisor)
    const payables = allocations.value.map((x:any,idx:number) => {
      const publicPart = round2(Number(x.occupancyWeight || 0) * averageDailyPrice)
      const payable = trunc0(privateAmounts[idx] + publicPart)
      const bill = selectedBillMap.value.get(Number(x.expenseBillId || 0))
      const billAmount = Number(bill?.amount || 0)
      const billUnits = Number(bill?.usageUnits || 0)
      const unitPrice = billUnits === 0 ? 0 : round2(billAmount / billUnits)
      x.calcDetail = {
        privateAmountText: `${buildPrivateUsageLine(x)}\n${round2(Number(x.tenantUnits || 0))}度 × 單價 ${unitPrice}元/度 = ${privateAmounts[idx]}元`,
        publicAmountText: `${Number(x.occupancyDays || 0)}日 × ${Number(x.occupantCount || 0)}人 × ${averageDailyPrice}元 = ${publicPart}元`,
        totalText: `${privateAmounts[idx]} + ${publicPart} = ${payable}元`,
        privateAmount: privateAmounts[idx],
        publicAmount: publicPart,
        payableAmount: payable
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

  if (calcMode.value === 4) {
    // 多錶平均單價：同一房源多張帳單加總金額與度數，取單一平均單價後再算各戶電費
    const unitPrice = aggregatedUnits.value === 0 ? 0 : round2(aggregatedAmount.value / aggregatedUnits.value)
    const privateAmounts = allocations.value.map((x:any) => round2(Number(x.tenantUnits || 0) * unitPrice))
    const privateTotal = round2(privateAmounts.reduce((s:number,v:number)=>s+v,0))
    const publicTotal = round2(aggregatedAmount.value - privateTotal)
    const divisor = allocations.value.reduce((s:number,x:any)=>s + Number(x.occupancyWeight || 0),0)
    const averageDailyPrice = divisor === 0 ? 0 : round2(publicTotal / divisor)
    const payables = allocations.value.map((x:any,idx:number) => {
      const publicPart = round2(Number(x.occupancyWeight || 0) * averageDailyPrice)
      const payable = trunc0(privateAmounts[idx] + publicPart)
      x.calcDetail = {
        privateAmountText: `${buildPrivateUsageLine(x)}\n${round2(Number(x.tenantUnits || 0))}度 × 平均單價 ${unitPrice}元/度 = ${privateAmounts[idx]}元`,
        publicAmountText: `${Number(x.occupancyDays || 0)}日 × ${Number(x.occupantCount || 0)}人 × ${averageDailyPrice}元 = ${publicPart}元`,
        totalText: `${privateAmounts[idx]} + ${publicPart} = ${payable}元`,
        privateAmount: privateAmounts[idx],
        publicAmount: publicPart,
        payableAmount: payable
      }
      return payable
    })
    result.value = {
      unitPrice,
      privateElectricityAmount: privateTotal,
      publicElectricityAmount: publicTotal,
      payableAmount: trunc2(payables.reduce((s:number,v:number)=>s+v,0)),
      tenantPayables: payables
    }
    return
  }

  if (calcMode.value === 1) {
    const unitPrice = round2(Number(manualUnitPrice.value || 0))
    const privateAmounts = allocations.value.map((x:any) => round2(Number(x.tenantUnits || 0) * unitPrice))
    const privateTotal = round2(privateAmounts.reduce((s:number,v:number)=>s+v,0))
    result.value = {
      unitPrice,
      privateElectricityAmount: privateTotal,
      publicElectricityAmount: 0,
      payableAmount: trunc2(privateTotal),
      tenantPayables: privateAmounts.map((x:number)=>trunc0(x))
    }
    allocations.value.forEach((x:any, idx:number) => {
      const payable = trunc0(privateAmounts[idx])
      x.calcDetail = {
        privateAmountText: `${buildPrivateUsageLine(x)}\n${round2(Number(x.tenantUnits || 0))}度 × 單價 ${unitPrice}元/度 = ${privateAmounts[idx]}元`,
        publicAmountText: '公電費：0元',
        totalText: `${privateAmounts[idx]} = ${payable}元`,
        privateAmount: privateAmounts[idx],
        publicAmount: 0,
        payableAmount: payable
      }
    })
    return
  }

}

const saveBill = async () => {
  notice.value = ''
  error.value = validate()
  if (error.value) return
  // 先以目前分攤資料重新試算，確保送出的金額與畫面一致（試算＝應收同一口徑）
  await calculate()
  if (error.value) return
  try {
    const primaryContractId = allocations.value.find((x:any) => Number(x.targetType) === 1 && x.contractId)?.contractId ?? null
    const { data } = await api.post('/electricity/bills', {
      contractId: primaryContractId,
      ruleType: calcMode.value,
      billingStartUtc: new Date(`${billingStart.value}T00:00:00Z`).toISOString(),
      billingEndUtc: new Date(`${billingEnd.value}T00:00:00Z`).toISOString(),
      totalAmount: aggregatedAmount.value,
      totalUnits: aggregatedUnits.value,
      unitPrice: aggregatedUnits.value === 0 ? 0 : aggregatedAmount.value / aggregatedUnits.value,
      allocations: allocations.value.map((x: any) => ({
        targetType: x.targetType,
        contractId: x.contractId,
        propertyRoomId: x.propertyRoomId,
        tenantId: x.tenantId,
        occupancyStartUtc: x.occupancyStartUtc,
        occupancyEndUtc: x.occupancyEndUtc,
        meterStart: x.meterStart,
        meterEnd: x.meterEnd,
        tenantUnits: x.tenantUnits,
        occupantCount: x.occupantCount,
        occupancyDays: x.occupancyDays,
        privateAmount: x.calcDetail?.privateAmount ?? null,
        publicAmount: x.calcDetail?.publicAmount ?? null,
        payableAmount: x.calcDetail?.payableAmount ?? null
      }))
    })
    const billId = Number(data?.billId || 0)
    if (billId > 0) {
      await api.post(`/electricity/bills/${billId}/create-charges`, {
        expenseBillIds: selectedExpenseBillIds.value
      })
      await loadExpenseBills()
      selectedExpenseBillIds.value = []
      resetPreview()
      notice.value = '儲存成功'
    } else {
      notice.value = '儲存成功'
    }
  } catch (e:any) {
    const message = e?.response?.data || e?.message || '儲存失敗'
    error.value = typeof message === 'string' ? message : JSON.stringify(message)
  }
}

onMounted(loadExpenseBills)
</script>
