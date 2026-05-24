<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">合約管理</h2>
    <div class="flex gap-2 mb-3">
      <input v-model="keyword" class="input input-bordered" placeholder="搜尋合約編號/房源" />
      <button class="btn" @click="load">查詢</button>
    </div>
    <div class="flex gap-2 mb-3">
      <input v-model="form.contractNo" class="input input-bordered" placeholder="合約編號" />
      <select v-model.number="selectedPropertyId" class="select select-bordered" @change="bindProperty">
        <option :value="0">選擇房源</option>
        <option v-for="p in properties" :key="p.id" :value="p.id">{{ p.code }} - {{ p.name }}</option>
      </select>
      <input v-model.number="form.monthlyRent" type="number" class="input input-bordered" placeholder="月租" />
      <button class="btn btn-primary" @click="save">{{ form.id ? '更新' : '新增' }}</button>
      <button v-if="form.id" class="btn" @click="reset">取消</button>
    </div>
    <table class="table table-zebra">
      <thead><tr><th>合約編號</th><th>房源</th><th>月租</th><th></th></tr></thead>
      <tbody>
        <tr v-for="c in items" :key="c.id">
          <td>{{ c.contractNo }}</td><td>{{ c.propertyName }}</td><td>{{ c.monthlyRent }}</td>
          <td class="flex gap-2 justify-end"><button class="btn btn-sm" @click="edit(c)">編輯</button><button class="btn btn-sm btn-error" @click="remove(c.id)">刪除</button></td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'
const items = ref<any[]>([])
const properties = ref<any[]>([])
const selectedPropertyId = ref(0)
const keyword = ref('')
const seed = ()=>({ id:0, contractNo:'', tenantId:1, propertyUnitId:null, propertyName:'', propertyAddress:'', startDateUtc:new Date().toISOString(), endDateUtc:new Date().toISOString(), monthlyRent:0, deposit:0, occupantCount:1, electricityRuleType:1, status:1 })
const form = ref<any>(seed())
const load = async()=>{ const {data}=await api.get('/contracts',{params:{keyword:keyword.value||undefined}}); items.value=data }
const loadProperties = async()=>{ const {data}=await api.get('/properties'); properties.value=data }
onMounted(async()=>{ await Promise.all([load(), loadProperties()]) })
const reset = ()=> { form.value = seed(); selectedPropertyId.value = 0 }
const edit = (c:any)=> { form.value = { ...c }; selectedPropertyId.value = c.propertyUnitId ?? 0 }
const bindProperty = ()=> {
  const p = properties.value.find((x:any)=>x.id===selectedPropertyId.value)
  if (!p) return
  form.value.propertyUnitId = p.id
  form.value.propertyName = p.name
  form.value.propertyAddress = p.address
}
const save = async()=>{ if (form.value.id) await api.put(`/contracts/${form.value.id}`, form.value); else await api.post('/contracts', form.value); reset(); await load() }
const remove = async(id:number)=>{ await api.delete(`/contracts/${id}`); await load() }
</script>
