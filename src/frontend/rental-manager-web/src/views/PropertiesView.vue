<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">房源管理</h2>
    <div class="flex gap-2 mb-3">
      <input v-model="keyword" class="input input-bordered" placeholder="搜尋代碼/名稱/地址" />
      <button class="btn" @click="load">查詢</button>
    </div>
    <div class="grid grid-cols-1 md:grid-cols-4 gap-2 mb-3">
      <input v-model="form.code" class="input input-bordered" placeholder="房源代碼" />
      <input v-model="form.name" class="input input-bordered" placeholder="房源名稱" />
      <input v-model="form.address" class="input input-bordered" placeholder="地址" />
      <div class="flex gap-2">
        <button class="btn btn-primary" @click="save">{{ form.id ? '更新' : '新增' }}</button>
        <button v-if="form.id" class="btn" @click="reset">取消</button>
      </div>
    </div>
    <table class="table table-zebra">
      <thead><tr><th>代碼</th><th>名稱</th><th>地址</th><th></th></tr></thead>
      <tbody>
        <tr v-for="p in items" :key="p.id">
          <td>{{ p.code }}</td><td>{{ p.name }}</td><td>{{ p.address }}</td>
          <td class="flex gap-2 justify-end"><button class="btn btn-sm" @click="edit(p)">編輯</button><button class="btn btn-sm btn-error" @click="remove(p.id)">刪除</button></td>
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
const seed = () => ({ id: 0, code: '', name: '', address: '', notes: '' })
const form = ref<any>(seed())
const load = async()=>{ const {data}=await api.get('/properties',{params:{keyword:keyword.value||undefined}}); items.value=data }
onMounted(load)
const reset = ()=> form.value = seed()
const edit = (p:any)=> form.value = { ...p }
const save = async()=>{ if(form.value.id) await api.put(`/properties/${form.value.id}`, form.value); else await api.post('/properties', form.value); reset(); await load() }
const remove = async(id:number)=>{ await api.delete(`/properties/${id}`); await load() }
</script>
