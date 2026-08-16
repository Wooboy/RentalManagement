<template>
  <div class="space-y-4">
    <PageHeader title="應收費用" :subtitle="`共 ${items.length} 筆`">
      <template #actions>
        <button class="btn btn-sm btn-primary" @click="openCreateModal">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
          新增應收
        </button>
      </template>
    </PageHeader>

    <div class="card bg-base-100 border border-base-300 shadow-sm p-3">
      <div class="flex flex-wrap items-end gap-3">
        <label class="form-control"><span class="label-text mb-1">查詢起日</span><input v-model="startDate" class="input input-bordered input-sm" type="date" max="2099-12-31" /></label>
        <label class="form-control"><span class="label-text mb-1">查詢迄日</span><input v-model="endDate" class="input input-bordered input-sm" type="date" max="2099-12-31" /></label>
        <label class="form-control"><span class="label-text mb-1">項目類別</span><select v-model.number="categoryFilter" class="select select-bordered select-sm">
          <option :value="0">全部</option><option v-for="option in chargeCategoryOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
        </select></label>
        <label class="form-control"><span class="label-text mb-1">收款狀態</span><select v-model="paidFilter" class="select select-bordered select-sm">
          <option value="all">全部</option><option value="paid">已收</option><option value="unpaid">未收</option>
        </select></label>
        <button class="btn btn-sm btn-ghost" @click="search">查詢</button>
      </div>
    </div>

    <div class="card bg-base-100 border border-base-300 shadow-sm overflow-hidden">
      <div class="overflow-x-auto">
        <table class="table">
          <thead><tr><th>合約</th><th>類別</th><th class="text-right">金額</th><th>發生日期</th><th>狀態</th><th class="text-right">操作</th></tr></thead>
          <tbody>
            <tr v-for="c in items" :key="c.id">
              <td class="font-medium">{{ c.contractName || c.contractNo || `#${c.contractId}` }}</td>
              <td>{{ chargeCategoryText(c.category) }}</td>
              <td class="text-right tabular-nums">{{ Number(c.amount || 0).toLocaleString() }}</td>
              <td>{{ c.occurredAtUtc?.slice(0,10) }}</td>
              <td><span class="badge badge-sm" :class="c.isPaid ? 'badge-success badge-soft' : 'badge-ghost'">{{ c.isPaid ? '已收' : '未收' }}</span></td>
              <td>
                <div class="flex gap-1 justify-end">
                  <button v-if="!c.isPaid" class="btn btn-xs btn-primary" @click="markPaid(c)">收款</button>
                  <button class="btn btn-xs btn-ghost" @click="openAttachments(c)">附件</button>
                  <button class="btn btn-xs btn-ghost" @click="edit(c)">編輯</button>
                  <button class="btn btn-xs btn-ghost text-error" @click="remove(c.id)">刪除</button>
                </div>
              </td>
            </tr>
            <tr v-if="items.length === 0"><td colspan="6" class="text-center text-base-content/50 py-10">查無應收費用資料。</td></tr>
          </tbody>
        </table>
      </div>
    </div>

    <AttachmentManager
      v-if="attachmentTarget"
      :entity-type="2"
      :entity-id="attachmentTarget.id"
      :subtitle="`應收：${attachmentTarget.contractName || attachmentTarget.contractNo || '#' + attachmentTarget.contractId}（${chargeCategoryText(attachmentTarget.category)}）`"
      @close="attachmentTarget = null"
    />

    <dialog class="modal" :class="{ 'modal-open': showModal }">
      <div class="modal-box max-w-4xl max-h-[85vh] overflow-y-auto">
        <h3 class="font-bold text-lg mb-3">{{ form.id ? '編輯應收' : '新增應收' }}</h3>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
          <label class="form-control"><span class="label-text mb-1">合約 *</span><select v-model.number="form.contractId" class="select select-bordered">
            <option :value="0">選擇合約 *</option>
            <option v-for="c in contracts" :key="c.id" :value="c.id">{{ c.contractName || c.propertyName || `#${c.id}` }}</option>
          </select></label>
          <label class="form-control"><span class="label-text mb-1">類別</span><select v-model.number="form.category" class="select select-bordered">
            <option v-for="option in chargeCategoryOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
          </select></label>
          <label class="form-control"><span class="label-text mb-1">金額 *</span><input v-model.number="form.amount" type="number" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">發生日期</span><input v-model="form.occurredDate" type="date" max="2099-12-31" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">狀態</span><select v-model.number="form.isPaid" class="select select-bordered">
            <option :value="0">未收</option><option :value="1">已收</option>
          </select></label>
          <label class="form-control"><span class="label-text mb-1">備註</span><input v-model="form.notes" class="input input-bordered" /></label>
        </div>
        <p class="text-error text-sm mt-3">{{ error }}</p>
        <div class="modal-action">
          <button class="btn" @click="closeModal">取消</button>
          <button class="btn btn-primary" @click="save">{{ form.id ? '更新' : '新增' }}</button>
        </div>
      </div>
    </dialog>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '../services/api'
import AttachmentManager from '../components/AttachmentManager.vue'
import PageHeader from '../components/PageHeader.vue'
const attachmentTarget = ref<any>(null)
const openAttachments = (c: any) => { attachmentTarget.value = c }
const route = useRoute()
const router = useRouter()
const startDate = ref('2000-01-01')
const endDate = ref('2099-12-31')
const categoryFilter = ref(0)
const paidFilter = ref<'all'|'paid'|'unpaid'>('unpaid')
const items = ref<any[]>([])
const contracts = ref<any[]>([])
const error = ref('')
const showModal = ref(false)
const chargeCategoryOptions = [
  { value: 1, label: '租金' },
  { value: 2, label: '水費' },
  { value: 3, label: '電費' },
  { value: 99, label: '其他' }
]
const toDateInput = (v: string) => (v ? v.slice(0,10) : '')
const toIsoDate = (v: string) => new Date(`${v}T00:00:00Z`).toISOString()
const seed = ()=>({ id:0, contractId:0, category:1, occurredDate:toDateInput(new Date().toISOString()), amount:0, notes:'', isPaid:0 })
const chargeCategoryText = (v:number) => chargeCategoryOptions.find(option => option.value === Number(v))?.label ?? String(v)
const form = ref<any>(seed())
const load = async()=>{
  const params:any = { startDateUtc: toIsoDate(startDate.value), endDateUtc: toIsoDate(endDate.value) }
  if (categoryFilter.value > 0) params.category = categoryFilter.value
  if (paidFilter.value === 'paid') params.isPaid = true
  if (paidFilter.value === 'unpaid') params.isPaid = false
  const {data}=await api.get('/charges',{params})
  items.value=data
}
const loadContracts = async()=>{ const {data}=await api.get('/contracts'); contracts.value=data }
const applyQueryFilter = () => {
  const qStart = String(route.query.startDate ?? '')
  const qEnd = String(route.query.endDate ?? '')
  const qCategory = Number(route.query.category ?? 0)
  const qPaid = String(route.query.paid ?? '')
  if (qStart) startDate.value = qStart
  if (qEnd) endDate.value = qEnd
  if (chargeCategoryOptions.some(option => option.value === qCategory)) categoryFilter.value = qCategory
  if (qPaid === 'paid' || qPaid === 'unpaid' || qPaid === 'all') paidFilter.value = qPaid
}
onMounted(async()=>{ applyQueryFilter(); await Promise.all([load(),loadContracts()]) })
const search = async()=>{
  await router.replace({ query: { startDate: startDate.value, endDate: endDate.value, category: categoryFilter.value > 0 ? String(categoryFilter.value) : undefined, paid: paidFilter.value } })
  await load()
}
const reset = ()=>{ form.value=seed(); error.value='' }
const openCreateModal = ()=>{ reset(); showModal.value = true }
const closeModal = ()=>{ showModal.value = false; reset() }
const edit = (c:any)=>{
  form.value={
    id:c.id, contractId:c.contractId, category:c.category, amount:c.amount, notes:c.notes ?? '',
    occurredDate:toDateInput(c.occurredAtUtc || c.billingStartUtc),
    isPaid:c.isPaid ? 1 : 0
  }
  error.value=''; showModal.value = true
}
const validate = ()=>{ if(!form.value.contractId) return '請選擇合約'; if(form.value.amount<0) return '金額不可小於0'; if(!form.value.occurredDate) return '請輸入發生日期'; return '' }
const save = async()=>{
  error.value=validate(); if(error.value) return
  const occurredIso = toIsoDate(form.value.occurredDate)
  const paid = Number(form.value.isPaid) === 1
  const payload={
    id: form.value.id || 0,
    contractId: form.value.contractId,
    category: form.value.category,
    occurredAtUtc: occurredIso,
    billingStartUtc: occurredIso,
    billingEndUtc: occurredIso,
    amount: form.value.amount,
    notes: form.value.notes,
    isPaid: paid,
    paidAtUtc: paid ? new Date().toISOString() : null
  }
  if(form.value.id) await api.put(`/charges/${form.value.id}`,payload); else await api.post('/charges',payload)
  closeModal(); await load()
}
const markPaid = async(c:any)=>{
  error.value = ''
  const payload = {
    ...c,
    isPaid: true,
    paidAtUtc: new Date().toISOString()
  }
  await api.put(`/charges/${c.id}`, payload)
  await load()
}
const remove = async(id:number)=>{ await api.delete(`/charges/${id}`); await load() }
</script>
