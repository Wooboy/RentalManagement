<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">應收費用</h2>
    <div class="flex gap-2 mb-3">
      <input v-model.number="year" class="input input-bordered" type="number" placeholder="年" />
      <input v-model.number="month" class="input input-bordered" type="number" placeholder="月" />
      <button class="btn" @click="load">查詢</button>
      <button class="btn btn-primary" @click="save">新增租金應收</button>
    </div>
    <table class="table table-zebra">
      <thead><tr><th>ID</th><th>金額</th><th>已收</th><th></th></tr></thead>
      <tbody>
        <tr v-for="c in items" :key="c.id">
          <td>{{ c.id }}</td><td>{{ c.amount }}</td><td>{{ c.isPaid ? '是' : '否' }}</td>
          <td class="flex gap-2 justify-end"><button class="btn btn-sm" @click="togglePaid(c)">切換已收</button><button class="btn btn-sm btn-error" @click="remove(c.id)">刪除</button></td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'
const items = ref<any[]>([])
const now = new Date()
const year = ref(now.getUTCFullYear())
const month = ref(now.getUTCMonth()+1)
const load = async()=>{ const {data}=await api.get('/charges',{params:{year:year.value,month:month.value}}); items.value=data }
onMounted(load)
const save = async()=>{ const d=new Date(Date.UTC(year.value,month.value-1,1)).toISOString(); await api.post('/charges',{contractId:1,category:1,billingStartUtc:d,billingEndUtc:d,amount:10000,isPaid:false}); await load() }
const togglePaid = async(c:any)=>{ await api.put(`/charges/${c.id}`,{...c,isPaid:!c.isPaid,paidAtUtc:!c.isPaid?new Date().toISOString():null}); await load() }
const remove = async(id:number)=>{ await api.delete(`/charges/${id}`); await load() }
</script>
