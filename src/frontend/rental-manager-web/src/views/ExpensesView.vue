<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">支出費用</h2>
    <div class="flex flex-wrap items-end gap-3 mb-3">
      <label class="form-control min-w-56"><span class="label-text mb-1">查詢起日</span><input v-model="searchStartDate" class="input input-bordered" type="date" max="2099-12-31" /></label>
      <label class="form-control min-w-56"><span class="label-text mb-1">查詢迄日</span><input v-model="searchEndDate" class="input input-bordered" type="date" max="2099-12-31" /></label>
      <label class="form-control min-w-44">
        <span class="label-text mb-1">項目類別</span>
        <select v-model.number="searchCategory" class="select select-bordered">
          <option :value="0">全部</option>
          <option v-for="option in expenseCategoryOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
        </select>
      </label>
      <label class="form-control min-w-44">
        <span class="label-text mb-1">分帳狀態</span>
        <select v-model.number="searchSplitStatus" class="select select-bordered">
          <option :value="0">全部</option>
          <option :value="1">未分帳</option>
          <option :value="2">已分帳</option>
          <option :value="3">無需分帳</option>
        </select>
      </label>
      <button class="btn" @click="search">查詢</button>
      <button class="btn btn-primary" @click="openCreateModal">新增</button>
    </div>

    <table class="table table-zebra">
      <thead><tr><th>房源</th><th>房間</th><th>類別</th><th>分帳狀態</th><th>金額</th><th>度數</th><th>發生日</th><th></th></tr></thead>
      <tbody>
        <tr v-for="e in items" :key="e.id">
          <td>{{ e.propertyUnitName || e.propertyUnitId }}</td>
          <td>{{ e.propertyRoomName || '-' }}</td>
          <td>{{ expenseCategoryText(e.category) }}</td><td>{{ splitStatusText(e.splitStatus) }}</td><td>{{ e.amount }}</td><td>{{ e.usageUnits ?? '-' }}</td><td>{{ e.occurredAtUtc?.slice(0,10) }}</td>
          <td class="flex gap-2 justify-end"><button class="btn btn-sm" @click="openAttachments(e)">附件</button><button class="btn btn-sm" @click="edit(e)">編輯</button><button class="btn btn-sm btn-error" @click="remove(e.id)">刪除</button></td>
        </tr>
      </tbody>
    </table>

    <AttachmentManager
      v-if="attachmentTarget"
      :entity-type="3"
      :entity-id="attachmentTarget.id"
      :subtitle="`支出：${attachmentTarget.propertyUnitName || attachmentTarget.propertyUnitId}（${expenseCategoryText(attachmentTarget.category)}）`"
      @close="attachmentTarget = null"
    />

    <dialog class="modal" :class="{ 'modal-open': showModal }">
      <div class="modal-box max-w-5xl max-h-[85vh] overflow-y-auto">
        <h3 class="font-bold text-lg mb-3">{{ form.id ? '編輯支出' : '新增支出' }}</h3>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
          <label class="form-control"><span class="label-text mb-1">房源 *</span><select v-model.number="form.propertyUnitId" class="select select-bordered" @change="onPropertyChange">
            <option :value="0">選擇房源</option>
            <option v-for="p in properties" :key="p.id" :value="p.id">{{ p.name }}</option>
          </select></label>
          <label class="form-control"><span class="label-text mb-1">房間（可選）</span><select v-model.number="form.propertyRoomId" class="select select-bordered">
            <option :value="0">不指定房間</option>
            <option v-for="r in rooms" :key="r.id" :value="r.id">{{ r.name }}</option>
          </select></label>
          <label class="form-control"><span class="label-text mb-1">支出類別</span><select v-model.number="form.category" class="select select-bordered">
            <option v-for="option in expenseCategoryOptions" :key="option.value" :value="option.value">{{ option.label }}</option>
          </select></label>
          <label class="form-control"><span class="label-text mb-1">發生日</span><input v-model="form.occurredAtUtc" type="date" max="2099-12-31" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">帳期起日</span><input v-model="form.billingStartUtc" type="date" max="2099-12-31" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">帳期迄日</span><input v-model="form.billingEndUtc" type="date" max="2099-12-31" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">金額 *</span><input v-model.number="form.amount" type="number" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">度數</span><input v-model.number="form.usageUnits" type="number" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">分帳狀態</span><select v-model.number="form.splitStatus" class="select select-bordered">
            <option :value="1">未分帳</option>
            <option :value="2">已分帳</option>
            <option :value="3">無需分帳</option>
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
const attachmentTarget = ref<any>(null)
const openAttachments = (e: any) => { attachmentTarget.value = e }
const route = useRoute()
const router = useRouter()
const searchStartDate = ref('2000-01-01')
const searchEndDate = ref('2099-12-31')
const searchCategory = ref(0)
const searchSplitStatus = ref(0)
const items = ref<any[]>([])
const properties = ref<any[]>([])
const rooms = ref<any[]>([])
const error = ref('')
const showModal = ref(false)
const expenseCategoryOptions = [
  { value: 1, label: '水費' },
  { value: 2, label: '電費' },
  { value: 3, label: '瓦斯' },
  { value: 4, label: '網路' },
  { value: 5, label: '電視' },
  { value: 6, label: '管理費' },
  { value: 7, label: '停車' },
  { value: 8, label: '稅金' },
  { value: 9, label: '修繕' },
  { value: 99, label: '其他' }
]
const defaultExpenseCategory = 2
const toDateInput = (v: string) => (v ? v.slice(0,10) : '')
const toIsoDate = (v: string) => {
  const d = new Date(`${v}T00:00:00Z`)
  if (Number.isNaN(d.getTime())) throw new Error('日期格式不正確')
  return d.toISOString()
}
const seed = ()=>({ id:0, propertyUnitId:0, propertyRoomId:0, category:defaultExpenseCategory, billingStartUtc:toDateInput(new Date().toISOString()), billingEndUtc:toDateInput(new Date().toISOString()), amount:0, usageUnits:null as number | null, splitStatus:1, notes:'', occurredAtUtc:toDateInput(new Date().toISOString()) })
const splitStatusText = (v:number) => v === 2 ? '已分帳' : v === 3 ? '無需分帳' : '未分帳'
const expenseCategoryText = (v:number) => expenseCategoryOptions.find(option => option.value === Number(v))?.label ?? String(v)
const form = ref<any>(seed())
const load = async()=>{
  const params:any = { startDateUtc: toIsoDate(searchStartDate.value), endDateUtc: toIsoDate(searchEndDate.value) }
  if (searchCategory.value > 0) params.category = searchCategory.value
  if (searchSplitStatus.value > 0) params.splitStatus = searchSplitStatus.value
  const {data}=await api.get('/expenses',{ params })
  items.value = data
}
const loadProperties = async()=>{ const {data}=await api.get('/properties'); properties.value=data }
const loadRooms = async()=>{ if(!form.value.propertyUnitId){ rooms.value=[]; return }; const {data}=await api.get('/rooms',{params:{propertyUnitId:form.value.propertyUnitId}}); rooms.value=data }
const applyQueryFilter = () => {
  const qStart = String(route.query.startDate ?? '')
  const qEnd = String(route.query.endDate ?? '')
  const qCategory = Number(route.query.category ?? 0)
  const qSplitStatus = Number(route.query.splitStatus ?? 0)
  if (qStart) searchStartDate.value = qStart
  if (qEnd) searchEndDate.value = qEnd
  if (expenseCategoryOptions.some(option => option.value === qCategory)) searchCategory.value = qCategory
  if ([1, 2, 3].includes(qSplitStatus)) searchSplitStatus.value = qSplitStatus
}
onMounted(async()=>{ applyQueryFilter(); await Promise.all([load(), loadProperties()]) })
const search = async()=>{
  await router.replace({
    query: {
      startDate: searchStartDate.value,
      endDate: searchEndDate.value,
      category: searchCategory.value > 0 ? String(searchCategory.value) : undefined,
      splitStatus: searchSplitStatus.value > 0 ? String(searchSplitStatus.value) : undefined
    }
  })
  await load()
}
const onPropertyChange = async()=>{ form.value.propertyRoomId=0; await loadRooms() }
const reset = ()=>{ form.value=seed(); rooms.value=[]; error.value='' }
const openCreateModal = ()=>{ reset(); showModal.value = true }
const closeModal = ()=>{ showModal.value = false; reset() }
const edit = async(e:any)=>{
  form.value = { id: e.id ?? 0, propertyUnitId: e.propertyUnitId ?? 0, propertyRoomId: e.propertyRoomId ?? 0, category: e.category ?? 1, billingStartUtc: toDateInput(e.billingStartUtc), billingEndUtc: toDateInput(e.billingEndUtc), amount: e.amount ?? 0, usageUnits: e.usageUnits ?? null, splitStatus: e.splitStatus ?? 1, notes: e.notes ?? '', occurredAtUtc: toDateInput(e.occurredAtUtc) }
  await loadRooms(); error.value=''; showModal.value = true
}
const validate = ()=>{ if(!form.value.propertyUnitId) return '請選擇房源'; if(form.value.amount<0) return '金額不可小於0'; if(!form.value.billingStartUtc||!form.value.billingEndUtc) return '請輸入帳期'; if(!form.value.occurredAtUtc) return '請輸入發生日'; if(form.value.billingEndUtc<form.value.billingStartUtc) return '結束日不可早於開始日'; return '' }
const save = async()=>{
  error.value=validate(); if(error.value) return
  try {
    const payload={...form.value, propertyRoomId:form.value.propertyRoomId||null, billingStartUtc:toIsoDate(form.value.billingStartUtc), billingEndUtc:toIsoDate(form.value.billingEndUtc), occurredAtUtc:toIsoDate(form.value.occurredAtUtc)}
    if(form.value.id) await api.put(`/expenses/${form.value.id}`, payload)
    else await api.post('/expenses', payload)
    closeModal(); await load()
  } catch (e: any) { error.value = e?.response?.data || e?.message || '新增失敗' }
}
const remove = async(id:number)=>{ await api.delete(`/expenses/${id}`); await load() }
</script>
