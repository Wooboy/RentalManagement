<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">應收費用</h2>
    <div class="flex gap-2 mb-3">
      <input v-model.number="year" class="input input-bordered" type="number" placeholder="年" />
      <input v-model.number="month" class="input input-bordered" type="number" placeholder="月" />
      <button class="btn" @click="load">查詢</button>
    </div>

    <div class="grid grid-cols-1 md:grid-cols-4 gap-2 mb-3">
      <select v-model.number="form.contractId" class="select select-bordered">
        <option :value="0">選擇合約 *</option>
        <option v-for="c in contracts" :key="c.id" :value="c.id">{{ c.contractNo }} - {{ c.propertyName }}</option>
      </select>
      <select v-model.number="form.category" class="select select-bordered">
        <option :value="1">租金</option><option :value="2">水費</option><option :value="3">電費</option><option :value="99">其他</option>
      </select>
      <input v-model="form.billingStartUtc" type="date" class="input input-bordered" />
      <input v-model="form.billingEndUtc" type="date" class="input input-bordered" />
      <input v-model.number="form.amount" type="number" class="input input-bordered" placeholder="金額 *" />
      <input v-model.number="form.meterStart" type="number" class="input input-bordered" placeholder="錶初讀數" />
      <input v-model.number="form.meterEnd" type="number" class="input input-bordered" placeholder="錶末讀數" />
      <input v-model="form.notes" class="input input-bordered" placeholder="備註" />
    </div>

    <div class="flex gap-2 mb-3">
      <button class="btn btn-primary" @click="save">{{ form.id ? '更新' : '新增' }}</button>
      <button v-if="form.id" class="btn" @click="reset">取消</button>
      <span class="text-error text-sm self-center">{{ error }}</span>
    </div>

    <table class="table table-zebra">
      <thead><tr><th>合約</th><th>類別</th><th>金額</th><th>用量</th><th>已收</th><th></th></tr></thead>
      <tbody>
        <tr v-for="c in items" :key="c.id">
          <td>{{ c.contractId }}</td><td>{{ c.category }}</td><td>{{ c.amount }}</td><td>{{ c.usageUnits ?? '-' }}</td><td>{{ c.isPaid ? '是' : '否' }}</td>
          <td class="flex gap-2 justify-end">
            <button class="btn btn-sm" @click="edit(c)">編輯</button>
            <button class="btn btn-sm" @click="togglePaid(c)">切換已收</button>
            <button class="btn btn-sm btn-error" @click="remove(c.id)">刪除</button>
          </td>
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
const contracts = ref<any[]>([])
const error = ref('')
const toDateInput = (v: string) => (v ? v.slice(0,10) : '')
const toIsoDate = (v: string) => new Date(`${v}T00:00:00Z`).toISOString()
const seed = ()=>({ id:0, contractId:0, category:1, billingStartUtc:toDateInput(new Date().toISOString()), billingEndUtc:toDateInput(new Date().toISOString()), amount:0, meterStart:null as number | null, meterEnd:null as number | null, notes:'', isPaid:false, paidAtUtc:null })
const form = ref<any>(seed())
const load = async()=>{ const {data}=await api.get('/charges',{params:{year:year.value,month:month.value}}); items.value=data }
const loadContracts = async()=>{ const {data}=await api.get('/contracts'); contracts.value=data }
onMounted(async()=>{ await Promise.all([load(),loadContracts()]) })
const reset = ()=>{ form.value=seed(); error.value='' }
const edit = (c:any)=>{ form.value={...c,billingStartUtc:toDateInput(c.billingStartUtc),billingEndUtc:toDateInput(c.billingEndUtc)}; error.value='' }
const validate = ()=>{ if(!form.value.contractId) return '請選擇合約'; if(form.value.amount<0) return '金額不可小於0'; if(!form.value.billingStartUtc||!form.value.billingEndUtc) return '請輸入帳期'; if(form.value.meterStart!=null && form.value.meterEnd!=null && form.value.meterEnd<form.value.meterStart) return '錶末不可小於錶初'; return '' }
const save = async()=>{ error.value=validate(); if(error.value) return; const payload={...form.value,billingStartUtc:toIsoDate(form.value.billingStartUtc),billingEndUtc:toIsoDate(form.value.billingEndUtc)}; if(form.value.id) await api.put(`/charges/${form.value.id}`,payload); else await api.post('/charges',payload); reset(); await load() }
const togglePaid = async(c:any)=>{ await api.put(`/charges/${c.id}`,{...c,isPaid:!c.isPaid,paidAtUtc:!c.isPaid?new Date().toISOString():null}); await load() }
const remove = async(id:number)=>{ await api.delete(`/charges/${id}`); await load() }
</script>
