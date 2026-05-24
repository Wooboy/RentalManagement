<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">合約管理</h2>
    <div class="flex gap-2 mb-3">
      <input v-model="keyword" class="input input-bordered" placeholder="搜尋合約編號/房源" />
      <button class="btn" @click="load">查詢</button>
    </div>
    <div class="flex gap-2 mb-3">
      <input v-model="form.contractNo" class="input input-bordered" placeholder="合約編號" />
      <input v-model="form.propertyName" class="input input-bordered" placeholder="房源名稱" />
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
const keyword = ref('')
const seed = ()=>({ id:0, contractNo:'', tenantId:1, propertyName:'', propertyAddress:'', startDateUtc:new Date().toISOString(), endDateUtc:new Date().toISOString(), monthlyRent:0, deposit:0, occupantCount:1, electricityRuleType:1, status:1 })
const form = ref<any>(seed())
const load = async()=>{ const {data}=await api.get('/contracts',{params:{keyword:keyword.value||undefined}}); items.value=data }
onMounted(load)
const reset = ()=> form.value = seed()
const edit = (c:any)=> form.value = { ...c }
const save = async()=>{ if (form.value.id) await api.put(`/contracts/${form.value.id}`, form.value); else await api.post('/contracts', form.value); reset(); await load() }
const remove = async(id:number)=>{ await api.delete(`/contracts/${id}`); await load() }
</script>
