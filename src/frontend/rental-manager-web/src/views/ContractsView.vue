<template>
  <div class="space-y-4">
    <PageHeader title="合約管理" :subtitle="`共 ${items.length} 份合約`">
      <template #actions>
        <button class="btn btn-sm btn-primary" @click="openCreateModal">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
          新增合約
        </button>
      </template>
    </PageHeader>

    <div class="card bg-base-100 border border-base-300 shadow-sm p-3">
      <div class="flex flex-wrap items-end gap-3">
        <label class="form-control">
          <span class="label-text mb-1">搜尋關鍵字</span>
          <input v-model="keyword" class="input input-bordered input-sm w-56" placeholder="合約名稱、編號、房源" @keyup.enter="load" />
        </label>
        <label class="form-control">
          <span class="label-text mb-1">狀態</span>
          <select v-model="statusFilter" class="select select-bordered select-sm">
            <option value="all">全部</option>
            <option value="1">生效中</option>
            <option value="3">已終止</option>
          </select>
        </label>
        <button class="btn btn-sm btn-ghost" @click="load">查詢</button>
        <div class="flex-1"></div>
        <button class="btn btn-sm btn-secondary" :disabled="!selectedContractIds.length" @click="openChargeMonthModal">
          建立本期應收<span v-if="selectedContractIds.length" class="ml-1">（{{ selectedContractIds.length }}）</span>
        </button>
      </div>
    </div>

    <p v-if="error" class="text-error text-sm">{{ error }}</p>
    <p v-if="notice" class="text-success text-sm">{{ notice }}</p>

    <div class="card bg-base-100 border border-base-300 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="table">
          <thead><tr><th><input type="checkbox" class="checkbox checkbox-sm" :checked="allVisibleSelected" @change="toggleAllVisible" /></th><th>合約名稱</th><th>租客</th><th>房源/房間</th><th>合約起日</th><th>合約迄日</th><th class="text-right">月租</th><th>付款間隔</th><th class="text-right">每期應付</th><th>備註</th><th class="text-right">操作</th></tr></thead>
          <tbody>
            <tr v-for="c in items" :key="c.id">
              <td><input type="checkbox" class="checkbox checkbox-sm" :value="c.id" v-model="selectedContractIds" /></td>
              <td class="font-medium">{{ c.contractName }}</td><td>{{ c.tenant?.name }}</td><td>{{ c.propertyName }}</td><td class="tabular-nums">{{ c.startDateUtc?.slice(0,10) }}</td><td class="tabular-nums">{{ c.endDateUtc?.slice(0,10) }}</td><td class="text-right tabular-nums">{{ Number(c.monthlyRent || 0).toLocaleString() }}</td><td>{{ paymentIntervalText(c.paymentIntervalMonths) }}</td><td class="text-right tabular-nums">{{ Number(c.periodPayableAmount || 0).toLocaleString() }}</td><td class="max-w-40 truncate text-base-content/70">{{ c.notes || '-' }}</td>
              <td>
                <div class="flex gap-1 justify-end">
                  <button class="btn btn-xs btn-ghost" @click="openAttachments(c)">附件</button>
                  <button class="btn btn-xs btn-ghost" @click="edit(c)">編輯</button>
                  <button class="btn btn-xs btn-ghost text-error" @click="remove(c.id)">刪除</button>
                </div>
              </td>
            </tr>
            <tr v-if="items.length === 0"><td colspan="11" class="text-center text-base-content/50 py-10">查無合約資料。</td></tr>
          </tbody>
        </table>
      </div>
    </div>

    <dialog class="modal" :class="{ 'modal-open': showModal }">
      <div class="modal-box max-w-5xl">
        <h3 class="font-bold text-lg mb-3">{{ form.id ? '編輯合約' : '新增合約' }}</h3>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-2 mb-3">
          <label class="form-control"><span class="label-text mb-1">合約名稱 *</span><input v-model="form.contractName" class="input input-bordered" placeholder="請輸入合約名稱" /></label>
          <label class="form-control"><span class="label-text mb-1">租客 *</span><select v-model.number="form.tenantId" class="select select-bordered">
            <option :value="0">選擇租客 *</option>
            <option v-for="t in tenants" :key="t.id" :value="t.id">{{ t.name }}</option>
          </select></label>
          <label class="form-control"><span class="label-text mb-1">月租 *</span><input v-model.number="form.monthlyRent" type="number" class="input input-bordered" placeholder="請輸入" /></label>
          <label class="form-control"><span class="label-text mb-1">付款間隔</span><select v-model.number="form.paymentIntervalMonths" class="select select-bordered">
            <option :value="1">每月</option><option :value="3">每季</option><option :value="12">每年</option>
          </select></label>
          <label class="form-control"><span class="label-text mb-1">每期應付</span><input :value="periodPayableAmount" type="number" class="input input-bordered" disabled /></label>
        </div>

        <div class="mb-3">
          <p class="text-sm mb-2">選擇房間（可跨房源多選）*</p>
          <div class="space-y-3">
            <div v-for="group in groupedRooms" :key="group.propertyUnitId" class="rounded border border-base-300 p-3">
              <div class="text-sm font-semibold mb-2">{{ group.propertyName }}</div>
              <div class="flex flex-wrap gap-3">
                <label v-for="r in group.rooms" :key="r.id" class="label cursor-pointer gap-2">
                  <input type="checkbox" class="checkbox checkbox-sm" :value="r.id" v-model="selectedRoomIds" />
                  <span>{{ r.name || r.code }}</span>
                </label>
              </div>
            </div>
          </div>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-4 gap-2">
          <label class="form-control"><span class="label-text mb-1">押金</span><input v-model.number="form.deposit" type="number" class="input input-bordered" placeholder="請輸入" /></label>
          <label class="form-control"><span class="label-text mb-1">居住人數 *</span><input v-model.number="form.occupantCount" type="number" class="input input-bordered" placeholder="請輸入" /></label>
          <label class="form-control"><span class="label-text mb-1">合約起始</span><input v-model="form.startDateUtc" type="date" max="2099-12-31" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">合約終止</span><input v-model="form.endDateUtc" type="date" max="2099-12-31" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">合約狀態</span><select v-model.number="form.status" class="select select-bordered">
            <option :value="1">狀態：生效中</option><option :value="3">狀態：已終止</option>
          </select></label>
          <label class="form-control md:col-span-3"><span class="label-text mb-1">備註</span><input v-model="form.notes" class="input input-bordered" placeholder="請輸入備註" /></label>
        </div>

        <div class="modal-action">
          <button class="btn" @click="closeModal">取消</button>
          <button class="btn btn-primary" @click="save">{{ form.id ? '更新' : '新增' }}</button>
        </div>
      </div>
    </dialog>

    <AttachmentManager
      v-if="attachmentTarget"
      :entity-type="1"
      :entity-id="attachmentTarget.id"
      :subtitle="`合約：${attachmentTarget.contractName || attachmentTarget.contractNo}`"
      @close="attachmentTarget = null"
    />

    <dialog class="modal" :class="{ 'modal-open': showChargeMonthModal }">
      <div class="modal-box max-w-md">
        <h3 class="font-bold text-lg mb-3">選擇建立應收月份</h3>
        <label class="form-control">
          <span class="label-text mb-1">月份 *</span>
          <input v-model="chargeMonth" type="month" class="input input-bordered" />
        </label>
        <p class="text-sm text-base-content/70 mt-3">將依合約起日的日建立所選月份的應收。</p>
        <div class="modal-action">
          <button class="btn" @click="closeChargeMonthModal">取消</button>
          <button class="btn btn-primary" @click="createPeriodCharges">建立</button>
        </div>
      </div>
    </dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import api from '../services/api'
import AttachmentManager from '../components/AttachmentManager.vue'
import PageHeader from '../components/PageHeader.vue'
const items = ref<any[]>([])
const attachmentTarget = ref<any>(null)
const openAttachments = (c: any) => { attachmentTarget.value = c }
const rooms = ref<any[]>([])
const tenants = ref<any[]>([])
const selectedRoomIds = ref<number[]>([])
const selectedContractIds = ref<number[]>([])
const keyword = ref('')
const statusFilter = ref<'all' | '1' | '3'>('1')
const showModal = ref(false)
const showChargeMonthModal = ref(false)
const error = ref('')
const notice = ref('')
const toDateInput = (v: string) => (v ? v.slice(0, 10) : '')
const toIsoDate = (v: string) => new Date(`${v}T00:00:00Z`).toISOString()
const toMonthInput = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
const seed = ()=>({ id:0, contractNo:'', contractName:'', tenantId:0, startDateUtc:toDateInput(new Date().toISOString()), endDateUtc:toDateInput(new Date().toISOString()), monthlyRent:0, paymentIntervalMonths:1, deposit:0, occupantCount:1, notes:'', status:1 })
const form = ref<any>(seed())
const chargeMonth = ref(toMonthInput(new Date()))
const periodPayableAmount = computed(() => Number(form.value.monthlyRent || 0) * Number(form.value.paymentIntervalMonths || 1))
const allVisibleSelected = computed(() => items.value.length > 0 && items.value.every((x:any) => selectedContractIds.value.includes(x.id)))
const groupedRooms = computed(() => {
  const groups = new Map<number, { propertyUnitId: number, propertyName: string, rooms: any[] }>()
  for (const room of rooms.value) {
    const propertyUnitId = room.propertyUnitId
    const propertyName = room.propertyUnit?.name || `房源 ${propertyUnitId}`
    if (!groups.has(propertyUnitId)) groups.set(propertyUnitId, { propertyUnitId, propertyName, rooms: [] })
    groups.get(propertyUnitId)!.rooms.push(room)
  }
  return Array.from(groups.values())
})
const paymentIntervalText = (m:number)=> m===12 ? '每年' : m===3 ? '每季' : '每月'
const load = async()=>{
  notice.value = ''
  const params:any = { keyword: keyword.value || undefined }
  if (statusFilter.value !== 'all') params.status = Number(statusFilter.value)
  const {data}=await api.get('/contracts',{params})
  items.value=data
  selectedContractIds.value = selectedContractIds.value.filter(id => items.value.some((x:any) => x.id === id))
}
const loadRooms = async()=>{ const {data}=await api.get('/rooms'); rooms.value=data }
const loadTenants = async()=>{ const {data}=await api.get('/tenants'); tenants.value=data }
onMounted(async()=>{ await Promise.all([load(), loadRooms(), loadTenants()]) })
const reset = ()=>{ form.value=seed(); selectedRoomIds.value=[]; error.value='' }
const openCreateModal = ()=>{ reset(); showModal.value = true }
const closeModal = ()=>{ showModal.value = false; reset() }
const edit = async(c:any)=>{ form.value={...c,startDateUtc:toDateInput(c.startDateUtc),endDateUtc:toDateInput(c.endDateUtc)}; await loadRooms(); selectedRoomIds.value=c.propertyRoomIds||[]; error.value=''; showModal.value = true }
const validate = ()=>{ if(!form.value.contractName?.trim()) return '請輸入合約名稱'; if(!form.value.tenantId) return '請選擇租客'; if(!selectedRoomIds.value.length) return '請至少選擇一間房間'; if(form.value.monthlyRent<0) return '月租不可小於0'; if(![1,3,12].includes(Number(form.value.paymentIntervalMonths||0))) return '付款間隔僅允許每月/每季/每年'; return '' }
const save = async()=>{ error.value=validate(); if(error.value) return; const payload={ contractNo:form.value.contractNo, contractName:form.value.contractName, tenantId:form.value.tenantId, propertyRoomIds:selectedRoomIds.value, startDateUtc:toIsoDate(form.value.startDateUtc), endDateUtc:toIsoDate(form.value.endDateUtc), monthlyRent:form.value.monthlyRent, paymentIntervalMonths:form.value.paymentIntervalMonths, deposit:form.value.deposit, occupantCount:form.value.occupantCount, notes:form.value.notes, electricityRuleType:3, status:form.value.status }; if(form.value.id) await api.put(`/contracts/${form.value.id}`,payload); else await api.post('/contracts',payload); closeModal(); await load() }
const remove = async(id:number)=>{ await api.delete(`/contracts/${id}`); await load() }
const selectAll = ()=>{ selectedContractIds.value = items.value.map((x:any) => x.id) }
const clearSelection = ()=>{ selectedContractIds.value = [] }
const toggleAllVisible = ()=>{ if (allVisibleSelected.value) clearSelection(); else selectAll() }
const openChargeMonthModal = ()=>{
  error.value = ''
  notice.value = ''
  if (!selectedContractIds.value.length) {
    error.value = '請先勾選至少一份合約'
    return
  }
  chargeMonth.value = toMonthInput(new Date())
  showChargeMonthModal.value = true
}
const closeChargeMonthModal = ()=>{ showChargeMonthModal.value = false }
const createPeriodCharges = async()=>{
  error.value = ''
  notice.value = ''
  if (!chargeMonth.value) {
    error.value = '請選擇月份'
    return
  }
  try {
    const { data } = await api.post('/contracts/batch-create-period-charges', {
      contractIds: selectedContractIds.value,
      targetMonthUtc: toIsoDate(`${chargeMonth.value}-01`)
    })
    const createdCount = Number(data?.createdCount || 0)
    const skippedCount = Number(data?.skippedCount || 0)
    notice.value = `已建立 ${createdCount} 筆本期應收` + (skippedCount > 0 ? `，略過 ${skippedCount} 筆` : '')
    closeChargeMonthModal()
  } catch (e:any) {
    error.value = e?.response?.data || e?.message || '建立本期應收失敗'
  }
}
</script>
