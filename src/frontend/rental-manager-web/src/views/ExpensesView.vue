<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">支出費用</h2>
    <div class="flex gap-2 mb-3">
      <input v-model.number="year" class="input input-bordered" type="number" placeholder="年" />
      <input v-model.number="month" class="input input-bordered" type="number" placeholder="月" />
      <button class="btn" @click="load">查詢</button>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-4 gap-2 mb-3">
      <select v-model.number="form.category" class="select select-bordered">
        <option :value="1">水費</option><option :value="2">電費</option><option :value="3">瓦斯</option><option :value="4">網路</option><option :value="5">電視</option><option :value="6">管理費</option><option :value="7">停車</option><option :value="8">稅金</option><option :value="9">修繕</option><option :value="99">其他</option>
      </select>
      <input v-model="form.billingStartUtc" type="date" class="input input-bordered" />
      <input v-model="form.billingEndUtc" type="date" class="input input-bordered" />
      <input v-model.number="form.amount" type="number" class="input input-bordered" placeholder="金額 *" />
      <input v-model="form.notes" class="input input-bordered" placeholder="備註" />
      <input v-model="form.occurredAtUtc" type="date" class="input input-bordered" />
    </div>

    <div class="flex gap-2 mb-3">
      <button class="btn btn-primary" @click="save">{{ form.id ? '更新' : '新增' }}</button>
      <button v-if="form.id" class="btn" @click="reset">取消</button>
      <span class="text-error text-sm self-center">{{ error }}</span>
    </div>

    <table class="table table-zebra">
      <thead><tr><th>類別</th><th>金額</th><th>發生日</th><th></th></tr></thead>
      <tbody>
        <tr v-for="e in items" :key="e.id">
          <td>{{ e.category }}</td><td>{{ e.amount }}</td><td>{{ e.occurredAtUtc?.slice(0,10) }}</td>
          <td class="flex gap-2 justify-end"><button class="btn btn-sm" @click="edit(e)">編輯</button><button class="btn btn-sm btn-error" @click="remove(e.id)">刪除</button></td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'
const now = new Date()
const year = ref(now.getUTCFullYear())
const month = ref(now.getUTCMonth()+1)
const items = ref<any[]>([])
const error = ref('')
const toDateInput = (v: string) => (v ? v.slice(0,10) : '')
const toIsoDate = (v: string) => new Date(`${v}T00:00:00Z`).toISOString()
const seed = ()=>({ id:0, category:1, billingStartUtc:toDateInput(new Date().toISOString()), billingEndUtc:toDateInput(new Date().toISOString()), amount:0, notes:'', occurredAtUtc:toDateInput(new Date().toISOString()) })
const form = ref<any>(seed())
const load = async()=>{ const {data}=await api.get('/expenses',{params:{year:year.value,month:month.value}}); items.value=data }
onMounted(load)
const reset = ()=>{ form.value=seed(); error.value='' }
const edit = (e:any)=>{ form.value={...e,billingStartUtc:toDateInput(e.billingStartUtc),billingEndUtc:toDateInput(e.billingEndUtc),occurredAtUtc:toDateInput(e.occurredAtUtc)}; error.value='' }
const validate = ()=>{ if(form.value.amount<0) return '金額不可小於0'; if(!form.value.billingStartUtc||!form.value.billingEndUtc) return '請輸入帳期'; if(form.value.billingEndUtc<form.value.billingStartUtc) return '結束日不可早於開始日'; return '' }
const save = async()=>{ error.value=validate(); if(error.value) return; const payload={...form.value,billingStartUtc:toIsoDate(form.value.billingStartUtc),billingEndUtc:toIsoDate(form.value.billingEndUtc),occurredAtUtc:toIsoDate(form.value.occurredAtUtc)}; if(form.value.id) await api.put(`/expenses/${form.value.id}`, payload); else await api.post('/expenses', payload); reset(); await load() }
const remove = async(id:number)=>{ await api.delete(`/expenses/${id}`); await load() }
</script>
