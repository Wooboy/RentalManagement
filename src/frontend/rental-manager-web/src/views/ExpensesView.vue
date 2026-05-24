<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">支出費用</h2>
    <div class="flex gap-2 mb-3">
      <input v-model.number="year" class="input input-bordered" type="number" placeholder="年" />
      <input v-model.number="month" class="input input-bordered" type="number" placeholder="月" />
      <button class="btn" @click="load">查詢</button>
      <button class="btn btn-primary" @click="save">新增支出</button>
    </div>
    <table class="table table-zebra">
      <thead><tr><th>ID</th><th>類別</th><th>金額</th><th></th></tr></thead>
      <tbody>
        <tr v-for="e in items" :key="e.id">
          <td>{{ e.id }}</td><td>{{ e.category }}</td><td>{{ e.amount }}</td>
          <td class="flex gap-2 justify-end"><button class="btn btn-sm" @click="edit(e)">編輯</button><button class="btn btn-sm btn-error" @click="remove(e.id)">刪除</button></td>
        </tr>
      </tbody>
    </table>
    <div class="mt-4 flex gap-2">
      <input v-model.number="form.category" type="number" class="input input-bordered" placeholder="類別" />
      <input v-model.number="form.amount" type="number" class="input input-bordered" placeholder="金額" />
      <button class="btn btn-secondary" @click="update">更新選取</button>
    </div>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'
const items = ref<any[]>([])
const now = new Date()
const year = ref(now.getUTCFullYear())
const month = ref(now.getUTCMonth()+1)
const form = ref<any>({id:0,category:1,amount:500})
const load = async()=>{ const {data}=await api.get('/expenses',{params:{year:year.value,month:month.value}}); items.value=data }
onMounted(load)
const save = async()=>{ const d=new Date(Date.UTC(year.value,month.value-1,1)).toISOString(); await api.post('/expenses',{category:1,billingStartUtc:d,billingEndUtc:d,amount:500,occurredAtUtc:d}); await load() }
const edit = (e:any)=> form.value = { ...e }
const update = async()=>{ if(!form.value.id) return; await api.put(`/expenses/${form.value.id}`, form.value); await load() }
const remove = async(id:number)=>{ await api.delete(`/expenses/${id}`); await load() }
</script>
