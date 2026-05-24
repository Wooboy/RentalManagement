<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">應收費用</h2>
    <div class="flex flex-wrap items-end gap-3 mb-3">
      <label class="form-control min-w-56"><span class="label-text mb-1">查詢起日</span><input v-model="startDate" class="input input-bordered" type="date" max="2099-12-31" /></label>
      <label class="form-control min-w-56"><span class="label-text mb-1">查詢迄日</span><input v-model="endDate" class="input input-bordered" type="date" max="2099-12-31" /></label>
      <label class="form-control min-w-40"><span class="label-text mb-1">收款狀態</span><select v-model="paidFilter" class="select select-bordered">
        <option value="all">全部</option><option value="paid">已收</option><option value="unpaid">未收</option>
      </select></label>
      <button class="btn" @click="search">查詢</button>
      <button class="btn btn-primary" @click="openCreateModal">新增</button>
    </div>

    <table class="table table-zebra">
      <thead><tr><th>合約</th><th>類別</th><th>金額</th><th>發生日期</th><th>狀態</th><th></th></tr></thead>
      <tbody>
        <tr v-for="c in items" :key="c.id">
          <td>{{ c.contractId }}</td><td>{{ c.category }}</td><td>{{ c.amount }}</td><td>{{ c.billingStartUtc?.slice(0,10) }}</td><td>{{ c.isPaid ? '已收' : '未收' }}</td>
          <td class="flex gap-2 justify-end">
            <button class="btn btn-sm" @click="edit(c)">編輯</button>
            <button class="btn btn-sm btn-error" @click="remove(c.id)">刪除</button>
          </td>
        </tr>
      </tbody>
    </table>

    <dialog class="modal" :class="{ 'modal-open': showModal }">
      <div class="modal-box max-w-4xl max-h-[85vh] overflow-y-auto">
        <h3 class="font-bold text-lg mb-3">{{ form.id ? '編輯應收' : '新增應收' }}</h3>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
          <label class="form-control"><span class="label-text mb-1">合約 *</span><select v-model.number="form.contractId" class="select select-bordered">
            <option :value="0">選擇合約 *</option>
            <option v-for="c in contracts" :key="c.id" :value="c.id">{{ c.propertyName }}</option>
          </select></label>
          <label class="form-control"><span class="label-text mb-1">類別</span><select v-model.number="form.category" class="select select-bordered">
            <option :value="1">租金</option><option :value="2">水費</option><option :value="3">電費</option><option :value="99">其他</option>
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
const route = useRoute()
const router = useRouter()
const startDate = ref('2000-01-01')
const endDate = ref('2099-12-31')
const paidFilter = ref<'all'|'paid'|'unpaid'>('all')
const items = ref<any[]>([])
const contracts = ref<any[]>([])
const error = ref('')
const showModal = ref(false)
const toDateInput = (v: string) => (v ? v.slice(0,10) : '')
const toIsoDate = (v: string) => new Date(`${v}T00:00:00Z`).toISOString()
const seed = ()=>({ id:0, contractId:0, category:1, occurredDate:toDateInput(new Date().toISOString()), amount:0, notes:'', isPaid:0 })
const form = ref<any>(seed())
const load = async()=>{
  const params:any = { startDateUtc: toIsoDate(startDate.value), endDateUtc: toIsoDate(endDate.value) }
  if (paidFilter.value === 'paid') params.isPaid = true
  if (paidFilter.value === 'unpaid') params.isPaid = false
  const {data}=await api.get('/charges',{params})
  items.value=data
}
const loadContracts = async()=>{ const {data}=await api.get('/contracts'); contracts.value=data }
const applyQueryFilter = () => {
  const qStart = String(route.query.startDate ?? '')
  const qEnd = String(route.query.endDate ?? '')
  const qPaid = String(route.query.paid ?? '')
  if (qStart) startDate.value = qStart
  if (qEnd) endDate.value = qEnd
  if (qPaid === 'paid' || qPaid === 'unpaid' || qPaid === 'all') paidFilter.value = qPaid
}
onMounted(async()=>{ applyQueryFilter(); await Promise.all([load(),loadContracts()]) })
const search = async()=>{ await router.replace({ query: { startDate: startDate.value, endDate: endDate.value, paid: paidFilter.value } }); await load() }
const reset = ()=>{ form.value=seed(); error.value='' }
const openCreateModal = ()=>{ reset(); showModal.value = true }
const closeModal = ()=>{ showModal.value = false; reset() }
const edit = (c:any)=>{
  form.value={
    id:c.id, contractId:c.contractId, category:c.category, amount:c.amount, notes:c.notes ?? '',
    occurredDate:toDateInput(c.billingStartUtc),
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
const remove = async(id:number)=>{ await api.delete(`/charges/${id}`); await load() }
</script>
