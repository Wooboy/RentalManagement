<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">支出費用</h2>
    <div class="flex gap-2 mb-3">
      <label class="form-control"><span class="label-text mb-1">查詢起日</span><input v-model="searchStartDate" class="input input-bordered" type="date" max="2099-12-31" /></label>
      <label class="form-control"><span class="label-text mb-1">查詢迄日</span><input v-model="searchEndDate" class="input input-bordered" type="date" max="2099-12-31" /></label>
      <button class="btn" @click="load">查詢</button>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-4 gap-2 mb-3">
      <label class="form-control"><span class="label-text mb-1">房源 *</span><select v-model.number="form.propertyUnitId" class="select select-bordered" @change="onPropertyChange">
        <option :value="0">選擇房源</option>
        <option v-for="p in properties" :key="p.id" :value="p.id">{{ p.code }} - {{ p.name }}</option>
      </select></label>
      <label class="form-control"><span class="label-text mb-1">房間（可選）</span><select v-model.number="form.propertyRoomId" class="select select-bordered">
        <option :value="0">不指定房間</option>
        <option v-for="r in rooms" :key="r.id" :value="r.id">{{ r.code }} - {{ r.name }}</option>
      </select></label>

      <label class="form-control"><span class="label-text mb-1">支出類別</span><select v-model.number="form.category" class="select select-bordered">
        <option :value="1">水費</option><option :value="2">電費</option><option :value="3">瓦斯</option><option :value="4">網路</option><option :value="5">電視</option><option :value="6">管理費</option><option :value="7">停車</option><option :value="8">稅金</option><option :value="9">修繕</option><option :value="99">其他</option>
      </select></label>
      <label class="form-control"><span class="label-text mb-1">帳期起日</span><input v-model="form.billingStartUtc" type="date" max="2099-12-31" class="input input-bordered" /></label>
      <label class="form-control"><span class="label-text mb-1">帳期迄日</span><input v-model="form.billingEndUtc" type="date" max="2099-12-31" class="input input-bordered" /></label>
      <label class="form-control"><span class="label-text mb-1">金額 *</span><input v-model.number="form.amount" type="number" class="input input-bordered" placeholder="請輸入" /></label>
      <label class="form-control"><span class="label-text mb-1">度數（電費用）</span><input v-model.number="form.usageUnits" type="number" class="input input-bordered" placeholder="請輸入" /></label>
      <label class="form-control"><span class="label-text mb-1">備註</span><input v-model="form.notes" class="input input-bordered" placeholder="請輸入" /></label>
      <label class="form-control"><span class="label-text mb-1">發生日</span><input v-model="form.occurredAtUtc" type="date" max="2099-12-31" class="input input-bordered" /></label>
    </div>

    <div class="flex gap-2 mb-3">
      <button class="btn btn-primary" @click="save">{{ form.id ? '更新' : '新增' }}</button>
      <button v-if="form.id" class="btn" @click="reset">取消</button>
      <span class="text-error text-sm self-center">{{ error }}</span>
    </div>

    <table class="table table-zebra">
      <thead><tr><th>房源</th><th>房間</th><th>類別</th><th>金額</th><th>度數</th><th>發生日</th><th></th></tr></thead>
      <tbody>
        <tr v-for="e in items" :key="e.id">
          <td>{{ e.propertyUnitName || e.propertyUnitId }}</td>
          <td>{{ e.propertyRoomName || '-' }}</td>
          <td>{{ e.category }}</td><td>{{ e.amount }}</td><td>{{ e.usageUnits ?? '-' }}</td><td>{{ e.occurredAtUtc?.slice(0,10) }}</td>
          <td class="flex gap-2 justify-end"><button class="btn btn-sm" @click="edit(e)">編輯</button><button class="btn btn-sm btn-error" @click="remove(e.id)">刪除</button></td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'
const searchStartDate = ref('2000-01-01')
const searchEndDate = ref('2099-12-31')
const items = ref<any[]>([])
const properties = ref<any[]>([])
const rooms = ref<any[]>([])
const error = ref('')
const toDateInput = (v: string) => (v ? v.slice(0,10) : '')
const toIsoDate = (v: string) => {
  const d = new Date(`${v}T00:00:00Z`)
  if (Number.isNaN(d.getTime())) throw new Error('日期格式不正確')
  return d.toISOString()
}
const seed = ()=>({ id:0, propertyUnitId:0, propertyRoomId:0, category:1, billingStartUtc:toDateInput(new Date().toISOString()), billingEndUtc:toDateInput(new Date().toISOString()), amount:0, usageUnits:null as number | null, notes:'', occurredAtUtc:toDateInput(new Date().toISOString()) })
const form = ref<any>(seed())
const load = async()=>{
  const {data}=await api.get('/expenses',{
    params:{
      startDateUtc: toIsoDate(searchStartDate.value),
      endDateUtc: toIsoDate(searchEndDate.value)
    }
  })
  items.value=data
  console.log('[Expenses] load items', data)
}
const loadProperties = async()=>{ const {data}=await api.get('/properties'); properties.value=data; console.log('[Expenses] load properties', data) }
const loadRooms = async()=>{
  if(!form.value.propertyUnitId){ rooms.value=[]; console.log('[Expenses] load rooms skipped (no propertyUnitId)'); return }
  const {data}=await api.get('/rooms',{params:{propertyUnitId:form.value.propertyUnitId}})
  rooms.value=data
  console.log('[Expenses] load rooms', { propertyUnitId: form.value.propertyUnitId, data })
}
onMounted(async()=>{ await Promise.all([load(), loadProperties()]) })
const onPropertyChange = async()=>{ form.value.propertyRoomId=0; console.log('[Expenses] property changed', form.value.propertyUnitId); await loadRooms() }
const reset = ()=>{ form.value=seed(); rooms.value=[]; error.value='' }
const edit = async(e:any)=>{
  form.value = {
    id: e.id ?? 0,
    propertyUnitId: e.propertyUnitId ?? 0,
    propertyRoomId: e.propertyRoomId ?? 0,
    category: e.category ?? 1,
    billingStartUtc: toDateInput(e.billingStartUtc),
    billingEndUtc: toDateInput(e.billingEndUtc),
    amount: e.amount ?? 0,
    usageUnits: e.usageUnits ?? null,
    notes: e.notes ?? '',
    occurredAtUtc: toDateInput(e.occurredAtUtc)
  }
  await loadRooms()
  error.value=''
}
const validate = ()=>{ if(!form.value.propertyUnitId) return '請選擇房源'; if(form.value.amount<0) return '金額不可小於0'; if(!form.value.billingStartUtc||!form.value.billingEndUtc) return '請輸入帳期'; if(!form.value.occurredAtUtc) return '請輸入發生日'; if(form.value.billingEndUtc<form.value.billingStartUtc) return '結束日不可早於開始日'; return '' }
const save = async()=>{
  error.value=validate()
  console.log('[Expenses] save validate result', error.value || 'ok')
  if(error.value) return
  try {
    const payload={
      ...form.value,
      propertyRoomId:form.value.propertyRoomId||null,
      billingStartUtc:toIsoDate(form.value.billingStartUtc),
      billingEndUtc:toIsoDate(form.value.billingEndUtc),
      occurredAtUtc:toIsoDate(form.value.occurredAtUtc)
    }
    console.log('[Expenses] save payload', payload)
    if(form.value.id) await api.put(`/expenses/${form.value.id}`, payload)
    else await api.post('/expenses', payload)
    console.log('[Expenses] save success')
    reset()
    await load()
  } catch (e: any) {
    console.error('[Expenses] save error', e)
    error.value = e?.response?.data || e?.message || '新增失敗'
  }
}
const remove = async(id:number)=>{ await api.delete(`/expenses/${id}`); await load() }
</script>
