<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">租客管理</h2>
    <div class="flex gap-2 mb-3">
      <input v-model="keyword" class="input input-bordered" placeholder="搜尋姓名/電話" />
      <button class="btn" @click="load">查詢</button>
    </div>
    <div class="flex gap-2 mb-3">
      <input v-model="form.name" class="input input-bordered" placeholder="姓名/公司" />
      <input v-model="form.phone" class="input input-bordered" placeholder="電話" />
      <button class="btn btn-primary" @click="save">{{ form.id ? '更新' : '新增' }}</button>
      <button v-if="form.id" class="btn" @click="reset">取消</button>
    </div>
    <table class="table table-zebra">
      <thead><tr><th>姓名</th><th>電話</th><th></th></tr></thead>
      <tbody>
        <tr v-for="t in items" :key="t.id">
          <td>{{ t.name }}</td><td>{{ t.phone }}</td>
          <td class="flex gap-2 justify-end">
            <button class="btn btn-sm" @click="edit(t)">編輯</button>
            <button class="btn btn-sm btn-error" @click="remove(t.id)">刪除</button>
          </td>
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
const form = ref<any>({ id: 0, name: '', phone: '', type: 1 })
const load = async()=>{ const {data}=await api.get('/tenants',{params:{keyword:keyword.value||undefined}}); items.value=data }
onMounted(load)
const reset = ()=> form.value = { id: 0, name: '', phone: '', type: 1 }
const edit = (t:any)=> form.value = { ...t }
const save = async()=>{
  if (form.value.id) await api.put(`/tenants/${form.value.id}`, form.value)
  else await api.post('/tenants', form.value)
  reset(); await load()
}
const remove = async(id:number)=>{ await api.delete(`/tenants/${id}`); await load() }
</script>
